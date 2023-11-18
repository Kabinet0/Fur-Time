Shader "Kabinet/ShellTexturingTriplanar"
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
            int _Density;
            

            // Integer Hash - III by iq. https://shadertoy.com/view/4tXyWN
            float hash(int2 x) // I'm mostly sure it's between 0-1
            {
                int2 q = 1103515245U * ((x >> 1U) ^ (x.yx));
                uint n = 1103515245U * ((q.x) ^ (q.y >> 3U));
                return float(n) * (1.0 / float(0xffffffffU));
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
                float heightAttenuation = (float)_ShellIndex / ((float)_ShellCount - 1);

                // Move verticies along normals
                float3 PositionOS = IN.positionOS.xyz;
                PositionOS += IN.normalOS.xyz * _ShellExtent * heightAttenuation;

                // Fragment shader outputs
                Varyings OUT;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(PositionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.normalWS = normalize(normalInputs.normalWS);
                //OUT.tangentWS = normalize(normalInputs.tangentWS);
                //OUT.bitangentWS = normalize(normalInputs.bitangentWS);

                OUT.uv = IN.uv;

                return OUT;
            }

            float InverseLerp(float start, float end, float val) {
                return (val - start) / (end - start);
            }

            // Good article: https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/
            float3 GetTriplanarWeights(float3 normal) {
                float3 triW = pow(abs(normal), 0.5);
                return triW / dot(triW, 1.0);
            }

            struct TriplanarUV {
                float2 x, y, z;
            };


            TriplanarUV GetTriplanarUV(float3 position) {
                TriplanarUV uv;

                uv.x = position.zy;
                
                uv.y = position.zx;
                uv.z = position.xy;

                //uv.x.x = -uv.x.x;
                //uv.y.x = -uv.y.x;

                return uv;
            }

            float2 getTriplanarHash(float3 position, float3 normal) {
                float3 Weights = GetTriplanarWeights(normal);
                Weights = abs(Weights);

                TriplanarUV TriUV = GetTriplanarUV(position);
                
                float3 HashResults = float3(
                    hash(floor(TriUV.x * uint(_Density))),
                    hash(floor(TriUV.y * uint(_Density))),
                    hash(floor(TriUV.z * uint(_Density)))
                );

                return TriUV.x * Weights.x +
                    TriUV.y * Weights.y;
                    TriUV.z * Weights.z;

                //return TriUV.x * Weights.x +
                //    TriUV.y * Weights.y +
                //    TriUV.z * Weights.z;

                //return (HashResults.x + HashResults.y + HashResults.z) / 3;
            }

            // The fragment shader definition.
            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // Convenience Variables
                float heightAttenuation = (float)_ShellIndex / (float)_ShellCount;
                float lowerShellAttenuation = saturate((float)(_ShellIndex - 1) / (float)_ShellCount);

                float2 startingCoords = getTriplanarHash(IN.positionWS, IN.normalWS);
                float hashValue = hash(floor(startingCoords * uint(_Density)));

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                //float fresnelEffect = (1 - dot(viewDirWS, IN.normalWS));

                half4 shellTopColor = lerp(_ShellOcclusionColor, _ShellColor, heightAttenuation);
                half4 shellBottomColor = lerp(_ShellOcclusionColor, _ShellColor, lowerShellAttenuation);

                half4 outputColor = shellTopColor;
                //half4 outputColor = half4(0, startingCoords.x, 0, 1);
                
                if (hashValue < heightAttenuation) { // Less than minimum height at given pixel
                    discard;
                }
                return outputColor;
            }


            

            ENDHLSL
        }
    }
}