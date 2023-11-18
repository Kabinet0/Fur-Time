Shader "Kabinet/ShellTexturing"
{
    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZTest Less
            ZWrite On
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Properties
            int _ShellIndex;
            int _ShellCount;
            float _ShellExtent;
            
            float4 _ShellColor;
            float4 _ShellOcclusionColor;
            float _Thickness;
            int _Density;

            float4 _DisplacementVector;
            float _DisplacementStrength;
            float _NormalTension;
            float _Curvature;
            float _DistanceAttenuation;

            float _JitterAmount;
            float _WindStrength;
            float4 _WindVector;
            

            // Integer Hash - III by iq. https://shadertoy.com/view/4tXyWN
            float hash(int2 x) // I'm mostly sure it's between 0-1
            {
                int2 q = 1103515245U * ((x >> 1U) ^ (x.yx));
                uint n = 1103515245U * ((q.x) ^ (q.y >> 3U));
                return float(n) * (1.0 / float(0xffffffffU));

            }

            // Fast pseudo-3D perlin nosie, courtesy of: https://www.shadertoy.com/view/MtcGRl
            float2 GetGradient(float2 intPos, float t) {
                // Uncomment for calculated rand
                float rand = frac(sin(dot(intPos, float2(12.9898, 78.233))) * 43758.5453);;

                // Texture-based rand (a bit faster on my GPU)
                //float rand = texture(iChannel0, intPos / 64.0).r;

                // Rotate gradient: random starting rotation, random rotation rate
                float angle = 6.283185 * rand + 4.0 * t * rand;
                return float2(cos(angle), sin(angle));
            }

            float Pseudo3dNoise(float3 pos) {
                float2 i = floor(pos.xy);
                float2 f = pos.xy - i;
                float2 blend = f * f * (3.0 - 2.0 * f);
                float noiseVal =
                    lerp(
                        lerp(
                            dot(GetGradient(i + float2(0, 0), pos.z), f - float2(0, 0)),
                            dot(GetGradient(i + float2(1, 0), pos.z), f - float2(1, 0)),
                            blend.x),
                        lerp(
                            dot(GetGradient(i + float2(0, 1), pos.z), f - float2(0, 1)),
                            dot(GetGradient(i + float2(1, 1), pos.z), f - float2(1, 1)),
                            blend.x),
                        blend.y
                    );
                return noiseVal / 0.7; // normalize to about [-1..1]
            }

            
            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD2;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                //float3 tangentWS    : TEXCOORD3;
                //float3 bitangentWS  : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                // Convenience Variables
                float heightNormalized = (float)_ShellIndex / ((float)_ShellCount - 1);
                heightNormalized = pow(heightNormalized, _DistanceAttenuation + 1);



                // Move verticies along normals
                float3 PositionOS = IN.positionOS.xyz;
                PositionOS += IN.normalOS.xyz * _ShellExtent * heightNormalized;

                float NormalAlignment = saturate(dot(IN.normalOS.xyz, normalize(_DisplacementVector)));
                float NormalAlignmentOpposite = saturate(dot(IN.normalOS.xyz, -normalize(_DisplacementVector)));

                NormalAlignment = min(NormalAlignment, NormalAlignmentOpposite);
                // Todo get a better name
                float CurvedHeight = pow(heightNormalized, max(1, _Curvature + 1));
                float HairDisplacementInfluence = lerp(CurvedHeight, CurvedHeight * NormalAlignment, _NormalTension);

                PositionOS += (_DisplacementVector * _DisplacementStrength * HairDisplacementInfluence);

                // Fragment shader outputs
                Varyings OUT;
                
                // Positions
                VertexPositionInputs positionInputs = GetVertexPositionInputs(PositionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                // Normals
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInputs.normalWS;

                

                OUT.uv = IN.uv;

                return OUT;
            }

            // The fragment shader definition.
            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // Convenience Variables
                float heightNormalized = (float)_ShellIndex / (float)_ShellCount;
                float lowerShellAttenuation = saturate((float)(_ShellIndex - 1) / (float)_ShellCount);

                // Packed wind noise density into 4th component of wind vector
                float noiseValue = (Pseudo3dNoise(float3((IN.positionWS + _WindVector.xyz * _Time.y) * _WindVector.w)) + 1) / 2;
                float CurvedHeight = pow(heightNormalized, max(1, _Curvature + 1));

                float2 startingCoords = (IN.uv * (float)_Density) + noiseValue * _WindStrength * CurvedHeight;
                float hashValue = hash(floor(startingCoords));
                float additionalHash = hash(floor(startingCoords) * 5);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                //float fresnelEffect = (1 - dot(viewDirWS, IN.normalWS));
                

                half4 shellTopColor = lerp(_ShellOcclusionColor, _ShellColor, pow(heightNormalized, 1.5));

                

                half4 outputColor = shellTopColor;
                
                float hairCentreDistance = length((frac(startingCoords) * 2 - 1) + (additionalHash - 0.5) * _JitterAmount);

                if (hairCentreDistance > _Thickness * (hashValue - heightNormalized) && _ShellIndex > 0) { // Less than minimum height at given pixel
                    discard;
                }

                //outputColor = half4(distance((frac(startingCoords) * 2 - 1), frac(startingCoords)), 0, 0, 1);
                return outputColor;
            }


            

            ENDHLSL
        }

        //Pass{
        //    Name "DepthOnly"
        //    Tags { "LightMode" = "DepthOnly" }

        //    ColorMask 0
        //    ZWrite On
        //    ZTest LEqual

        //    HLSLPROGRAM
        //    #pragma vertex DepthOnlyVertex
        //    #pragma fragment DepthOnlyFragment

        //    // Material Keywords
        //    #pragma shader_feature _ALPHATEST_ON
        //    #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

        //    // GPU Instancing
        //    #pragma multi_compile_instancing
        //    // #pragma multi_compile _ DOTS_INSTANCING_ON

        //    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
        //    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        //    #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
        //    ENDHLSL
        //}
    }
}