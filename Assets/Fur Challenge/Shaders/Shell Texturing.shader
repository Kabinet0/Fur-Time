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
            int _Density;
            

            // Integer Hash - III by iq. https://shadertoy.com/view/4tXyWN
            float hash(uint2 x) // I'm mostly sure it's between 0-1
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
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
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
                OUT.tangentWS = normalize(normalInputs.tangentWS);
                OUT.bitangentWS = normalize(normalInputs.bitangentWS);

                OUT.uv = IN.uv;

                return OUT;
            }

            // The fragment shader definition.
            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // Convenience Variables
                float heightAttenuation = (float)_ShellIndex / (float)_ShellCount;
                //float lowerHeightAttenuation = (float)(_ShellIndex - 1) / (float)_ShellCount;

                float2 startingCoords = IN.uv * uint(_Density);
                float hashValue = hash(floor(startingCoords));

                

                half4 outputColor = _ShellColor * heightAttenuation;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float fresnelEffect = (1 - dot(viewDirWS, IN.normalWS));



                // Is this even tangent space?
                float3 viewRayTS = -float3(dot(IN.tangentWS, viewDirWS), dot(IN.bitangentWS, viewDirWS), dot(IN.normalWS, viewDirWS)) * facing;
                viewRayTS = normalize(viewRayTS);

                // Algorithm from https://theshoemaker.de/posts/ray-casting-in-2d-grids
                // Thanks :D
                float2 dirSign = float2(1, 1);
                float2 tileOffset = float2(1, 1);
                if (viewRayTS.x < 0) {
                    dirSign.x = -1;
                    tileOffset.x = 0;
                }
                if (viewRayTS.y < 0) {
                    dirSign.y = -1;
                    tileOffset.y = 0;
                }
                float2 RayBias = 0.001f * dirSign;

                int2 tileCoords = floor(startingCoords);
                float2 dT = ((float2)(tileCoords + tileOffset) - startingCoords) / viewRayTS.xy;
                float2 ddT = dirSign / viewRayTS.xy;
                float t = 0;
                
                //outputColor = float4(heightAttenuation, 0, 0, 1);
                
                if (hashValue < heightAttenuation) { // Less than minimum height at given pixel
                    

                    int maxRaycastDistance = 1;
                    bool RaycastHit = false;
                    for (int i = 0; i < maxRaycastDistance; i++) {
                        


                        if (dT.x < dT.y) {
                            t += dT.x;

                            dT.x = ddT.x;
                            dT.y -= dT.x;
                        }
                        else {
                            t += dT.y;

                            dT.x -= dT.y;
                            dT.y = ddT.y;
                        }

                        hashValue = hash(startingCoords + viewRayTS.xy * t + RayBias);

                        hashValue += viewRayTS.z * t * _ShellExtent - RayBias;
                        float hitHeightAttenuation = heightAttenuation + (viewRayTS.z * t * _ShellExtent);
                        if (hashValue > heightAttenuation) {
                            //outputColor = _ShellColor * hitHeightAttenuation;
                            RaycastHit = true;
                            break;
                            
                        }
                        //else {
                        //    outputColor = float4(0, 0, 1, 1);
                        //}
                    }

                    if (!RaycastHit) {
                        discard;
                    }



                    

                    
                    //float HeightTest = heightAttenuation + viewRayTS.z * fresnelEffect;

                    
                }

                return outputColor;
            }


            

            ENDHLSL
        }
    }
}