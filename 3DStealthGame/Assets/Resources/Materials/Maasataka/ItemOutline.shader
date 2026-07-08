Shader "Custom/ItemOutline"
{
    Properties
    {
        _OutlineColor ("アウトラインの色", Color) = (1, 1, 0, 1)
        _OutlineWidth ("アウトラインの太さ", Range(0.0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+1" }

        // モデルを法線方向に膨らませて裏面だけ描画し、輪郭線に見せる
        Pass
        {
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                // 頂点を法線方向に押し出す
                v.vertex.xyz += normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}