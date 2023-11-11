Shader "Kabinet/ShellParalaxTexturing"
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

            float InverseLerp(float start, float end, float val) {
                return (val - start) / (end - start);
            }

            // Good article https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/
            float3 GetTriplanarWeights(float3 normal) {
                float3 triW = pow(abs(normal), 0.5);
                return triW / dot(triW, 1.0);
            }

            float2 getTriplanarCoordinate(float3 position, float3 normal) {
                float3 Weights = GetTriplanarWeights(normal);
                //float3 internalPos = abs(position);

                
                

                return position.zy * Weights.x +
                    position.xz * Weights.y +
                    position.xy * Weights.z;
            }

            // The fragment shader definition.
            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // Convenience Variables
                float heightAttenuation = (float)_ShellIndex / (float)_ShellCount;
                float lowerShellAttenuation = saturate((float)(_ShellIndex - 1) / (float)_ShellCount);

                float2 startingCoords = getTriplanarCoordinate(IN.positionWS, IN.normalWS) * uint(_Density);
                float hashValue = hash(floor(startingCoords));

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                //float fresnelEffect = (1 - dot(viewDirWS, IN.normalWS));

                half4 shellTopColor = lerp(_ShellOcclusionColor, _ShellColor, heightAttenuation);
                half4 shellBottomColor = lerp(_ShellOcclusionColor, _ShellColor, lowerShellAttenuation);

                half4 outputColor = shellTopColor;
                
                


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

                float2 currCoords = startingCoords;
                int2 tileCoords = floor(startingCoords);
                float2 dT;
                float2 ddT = dirSign / viewRayTS.xy;
                float t = 0;
                
                //outputColor = float4(heightAttenuation, 0, 0, 1);
                
                if (hashValue < heightAttenuation) { // Less than minimum height at given pixel
                    //discard;

                    int maxRaycastDistance = 4;
                    bool RaycastHit = false;
                    for (int i = 0; i < maxRaycastDistance; i++) {
                        dT = ((float2)(tileCoords + tileOffset) - currCoords) / viewRayTS.xy;


                        if (dT.x < dT.y) {
                            t += dT.x;

                            tileCoords.x += dirSign.x;
                        }
                        else {
                            t += dT.y;

                            tileCoords.y += dirSign.y;
                        }

                        currCoords += viewRayTS.xy * t;



                        hashValue = hash(floor(startingCoords + viewRayTS.xy * t + RayBias));

                        
                        
                        if (hashValue >= heightAttenuation) { // Ray hit something taller than this pixel's minimum height
                            

                            float hitHeight = heightAttenuation + (((viewRayTS.z * t) / _Density * 1/6) / _ShellExtent - RayBias);

                            

                            if (hitHeight > lowerShellAttenuation) {
                                RaycastHit = true;
                                float percentBetweenShells = InverseLerp(lowerShellAttenuation, heightAttenuation, hitHeight);
                                outputColor = lerp(shellBottomColor, shellTopColor, percentBetweenShells);
                                //outputColor = half4(hitHeight, 0, 0, 1);
                            }
                            
                            break;
                            
                        }
                    }

                    if (!RaycastHit) {
                        discard;
                    }
                }

                return outputColor;
            }


            

            ENDHLSL
        }
    }
}