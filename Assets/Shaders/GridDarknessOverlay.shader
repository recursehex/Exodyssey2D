Shader "Custom/GridDarknessOverlay"
{
	Properties
	{
		_BaseDarkColor ("Base Dark Color", Color) = (0,0,0,0.95)
		_NightVisionTint ("Night Vision Tint", Color) = (0.33,0.82,0.35,1)
		_Ambient ("Ambient", Range(0,1)) = 1
		_NightVisionStrength ("Night Vision Strength", Range(0,1)) = 0
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			#define MAX_LIGHT_SOURCES 32

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 worldPos : TEXCOORD0;
			};

			fixed4 _BaseDarkColor;
			fixed4 _NightVisionTint;
			float _Ambient;
			float _NightVisionStrength;
			float _LightCount;
			float4 _LightData[MAX_LIGHT_SOURCES];

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float4 world = mul(unity_ObjectToWorld, v.vertex);
				o.worldPos = world.xy;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float illumination = saturate(_Ambient);
				for (int index = 0; index < MAX_LIGHT_SOURCES; index++)
				{
					if (index >= (int)_LightCount)
						break;
					float4 lightData = _LightData[index];
					float radius = max(lightData.z, 0.0001);
					float distanceToLight = distance(i.worldPos, lightData.xy);
					float falloff = smoothstep(radius, 0, distanceToLight);
					illumination += falloff * lightData.w;
				}
				illumination = saturate(illumination);
				float darkness = 1.0 - illumination;
				float baseAlpha = darkness * _BaseDarkColor.a;
				float nvStrength = saturate(_NightVisionStrength);
				float nvAlpha = nvStrength * _NightVisionTint.a;
				float alpha = saturate(baseAlpha + nvAlpha * (1.0 - baseAlpha));
				float nvBlend = (alpha > 0.001) ? saturate(nvAlpha / alpha) : 0.0;
				fixed3 color = lerp(_BaseDarkColor.rgb, _NightVisionTint.rgb, nvBlend);
				return fixed4(color, alpha);
			}
			ENDCG
		}
	}
}
