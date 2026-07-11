Shader "Custom/GridDarknessOverlay"
{
	Properties
	{
		_BaseDarkColor ("Base Dark Color", Color) = (0,0,0,0.95)
		_NightVisionTint ("Night Vision Tint", Color) = (0.33,0.82,0.35,1)
		_Ambient ("Ambient", Range(0,1)) = 1
		_NightVisionStrength ("Night Vision Strength", Range(0,1)) = 0
		_LightEdgeFeather ("Light Edge Feather", Range(0.1,1.5)) = 1
		_LightCornerRadius ("Light Corner Radius", Range(0,0.5)) = 0.18
		_LightFalloffStrength ("Light Falloff Strength", Range(0.1,3)) = 0.9
		_MaxIllumination ("Maximum Illumination", Range(0,1)) = 1
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
			float _LightEdgeFeather;
			float _LightCornerRadius;
			float _LightFalloffStrength;
			float _MaxIllumination;
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

			float RoundedBoxDistance(float2 sourceOffset, float2 halfExtents, float cornerRadius)
			{
				float2 roundedOffset = sourceOffset - (halfExtents - cornerRadius);
				return length(max(roundedOffset, 0))
					+ min(max(roundedOffset.x, roundedOffset.y), 0)
					- cornerRadius;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float illumination = saturate(_Ambient);
				float availableLight = max(_MaxIllumination - illumination, 0);
				for (int index = 0; index < MAX_LIGHT_SOURCES; index++)
				{
					if (index >= (int)_LightCount)
						break;
					float4 lightData = _LightData[index];
					bool isVehicleBeam = lightData.z < 0;
					bool isCrossLight = !isVehicleBeam && lightData.w < 0;
					float squareHalfExtent = max(abs(lightData.z), 0.0001);
					float2 halfExtents = isVehicleBeam
						? float2(squareHalfExtent, 0.5)
						: float2(squareHalfExtent, squareHalfExtent);
					float cornerRadius = min(_LightCornerRadius, min(halfExtents.x, halfExtents.y));
					float2 sourceOffset = abs(i.worldPos - lightData.xy);
					float signedDistance = RoundedBoxDistance(sourceOffset, halfExtents, cornerRadius);
					if (isCrossLight)
					{
						float crossCornerRadius = min(_LightCornerRadius, 0.5);
						float horizontalDistance = RoundedBoxDistance(
							sourceOffset,
							float2(squareHalfExtent, 0.5),
							crossCornerRadius);
						float verticalDistance = RoundedBoxDistance(
							sourceOffset,
							float2(0.5, squareHalfExtent),
							crossCornerRadius);
						signedDistance = min(horizontalDistance, verticalDistance);
					}
					float halfFeather = _LightEdgeFeather * 0.5;
					float edgeProgress = saturate(
						(signedDistance + halfFeather) / max(_LightEdgeFeather, 0.0001));
					float smoothEdgeProgress = edgeProgress * edgeProgress * edgeProgress
						* (edgeProgress * (edgeProgress * 6.0 - 15.0) + 10.0);
					float edgeMask = 1.0 - smoothEdgeProgress;
					float distanceFromSource = isVehicleBeam
						? saturate((i.worldPos.x - (lightData.x - halfExtents.x)) / (halfExtents.x * 2.0))
						: saturate(max(
							sourceOffset.x / halfExtents.x,
							sourceOffset.y / halfExtents.y));
					if (isCrossLight)
					{
						float horizontalProgress = max(
							sourceOffset.x / squareHalfExtent,
							sourceOffset.y / 0.5);
						float verticalProgress = max(
							sourceOffset.x / 0.5,
							sourceOffset.y / squareHalfExtent);
						distanceFromSource = saturate(min(horizontalProgress, verticalProgress));
					}
					float interiorGradient = exp2(
						-_LightFalloffStrength * distanceFromSource * distanceFromSource);
					illumination += edgeMask * interiorGradient * abs(lightData.w) * availableLight;
				}
				illumination = min(saturate(illumination), _MaxIllumination);
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
