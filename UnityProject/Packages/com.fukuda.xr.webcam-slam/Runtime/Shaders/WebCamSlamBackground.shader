Shader "Unlit/WebCamSlamBackground"
{
    // AR camera background shader for the WebCam SLAM provider. The texture bound here
    // (see WebCamSlamCameraSubsystem.k_TexturePropertyName) has already been corrected for
    // sensor rotation/mirroring by WebCamFrameSource, so _UnityDisplayTransform is normally
    // the identity matrix; it is still applied so this shader behaves like any other AR
    // Foundation background shader (see XR Simulation's "Simulation Background Simple").
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        // ------------------------------------------------------------------
        // Built-in Render Pipeline
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest LEqual
            ZWrite On
            Lighting Off
            LOD 100
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4x4 _UnityDisplayTransform;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = mul(float3(v.uv, 1.0f), (float3x3)_UnityDisplayTransform).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }

    SubShader
    {
        // ------------------------------------------------------------------
        // Universal Render Pipeline
        // E2E verification of this path is deferred to a later phase; included so the
        // package does not break projects that have URP installed.
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.0"
        }

        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Cull Off
            ZTest LEqual
            ZWrite On
            LOD 100
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4x4 _UnityDisplayTransform;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = mul(float3(v.uv, 1.0f), (float3x3)_UnityDisplayTransform).xy;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
}
