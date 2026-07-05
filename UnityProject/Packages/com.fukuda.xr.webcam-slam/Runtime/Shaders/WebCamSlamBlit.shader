Shader "Hidden/WebCamSlamBlit"
{
    // Copies a WebCamTexture into a plain RenderTexture while correcting for the
    // source video's rotation (WebCamTexture.videoRotationAngle, 0/90/180/270) and
    // vertical mirroring (WebCamTexture.videoVerticallyMirrored), plus an optional
    // user-requested horizontal mirror. Used by WebCamFrameSource so that downstream
    // consumers (the background shader, XRTextureDescriptor) always see a
    // right-side-up, non-rotated frame.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
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
            // x: rotation in degrees (0/90/180/270), y: 1 if vertically mirrored, z: 1 if horizontally mirrored
            float4 _WebCamSlamBlitParams;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = v.uv;

                float rotation = _WebCamSlamBlitParams.x;
                if (rotation > 269)
                {
                    uv = float2(1 - uv.y, uv.x);
                }
                else if (rotation > 179)
                {
                    uv = float2(1 - uv.x, 1 - uv.y);
                }
                else if (rotation > 89)
                {
                    uv = float2(uv.y, 1 - uv.x);
                }

                if (_WebCamSlamBlitParams.y > 0.5)
                    uv.y = 1 - uv.y;

                if (_WebCamSlamBlitParams.z > 0.5)
                    uv.x = 1 - uv.x;

                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
