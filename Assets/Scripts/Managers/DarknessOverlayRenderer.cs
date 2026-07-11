using System.Collections.Generic;
using UnityEngine;

public sealed class DarknessOverlayRenderer
{
	private const int maxLightSources = 32;
	private readonly Vector4[] ShaderLightData = new Vector4[maxLightSources];
	private static readonly int ambientId = Shader.PropertyToID("_Ambient");
	private static readonly int baseDarkColorId = Shader.PropertyToID("_BaseDarkColor");
	private static readonly int nightVisionTintId = Shader.PropertyToID("_NightVisionTint");
	private static readonly int nightVisionStrengthId = Shader.PropertyToID("_NightVisionStrength");
	private static readonly int lightCountId = Shader.PropertyToID("_LightCount");
	private static readonly int lightDataId = Shader.PropertyToID("_LightData");
	private static readonly int lightEdgeFeatherId = Shader.PropertyToID("_LightEdgeFeather");
	private static readonly int lightCornerRadiusId = Shader.PropertyToID("_LightCornerRadius");
	private static readonly int lightFalloffStrengthId = Shader.PropertyToID("_LightFalloffStrength");
	private static readonly int maxIlluminationId = Shader.PropertyToID("_MaxIllumination");
	private GameObject OverlayObject;
	private Material OverlayMaterial;
	public MeshRenderer Renderer { get; private set; }
	public bool IsReady => OverlayMaterial != null;

	public void Initialize(Transform Parent, int layer)
	{
		if (OverlayObject != null)
			return;
		Shader OverlayShader = Shader.Find("Custom/GridDarknessOverlay");
		if (OverlayShader == null)
		{
			Debug.LogWarning("GridDarknessOverlay shader not found. Night visibility overlay disabled.");
			return;
		}
		OverlayObject = new GameObject("GridDarknessOverlay");
		OverlayObject.transform.SetParent(Parent, false);
		OverlayObject.layer = layer;
		MeshFilter MeshFilter = OverlayObject.AddComponent<MeshFilter>();
		Renderer = OverlayObject.AddComponent<MeshRenderer>();
		OverlayMaterial = new Material(OverlayShader);
		Renderer.sharedMaterial = OverlayMaterial;
		MeshFilter.sharedMesh = BuildOverlayMesh();
		SetActive(false);
	}

	public void Dispose()
	{
		if (OverlayMaterial != null)
			Object.Destroy(OverlayMaterial);
		if (OverlayObject != null)
			Object.Destroy(OverlayObject);
		OverlayMaterial = null;
		OverlayObject = null;
		Renderer = null;
	}

	public void SetActive(bool isActive)
	{
		if (Renderer != null)
			Renderer.enabled = isActive;
	}

	public void Apply(
		float ambient,
		float nightVisionStrength,
		float overlayDarkAlpha,
		Color NightVisionTint,
		float lightEdgeFeather,
		float lightCornerRadius,
		float lightFalloffStrength,
		float maxIllumination,
		IReadOnlyDictionary<int, Vector4> AppliedLights)
	{
		if (OverlayMaterial == null)
			return;
		int lightCount = 0;
		foreach (KeyValuePair<int, Vector4> Entry in AppliedLights)
		{
			if (lightCount >= maxLightSources)
				break;
			ShaderLightData[lightCount] = Entry.Value;
			lightCount++;
		}
		for (int i = lightCount; i < maxLightSources; i++)
			ShaderLightData[i] = Vector4.zero;
		OverlayMaterial.SetFloat(ambientId, ambient);
		OverlayMaterial.SetColor(baseDarkColorId, new Color(0f, 0f, 0f, overlayDarkAlpha));
		OverlayMaterial.SetColor(nightVisionTintId, NightVisionTint);
		OverlayMaterial.SetFloat(nightVisionStrengthId, nightVisionStrength);
		OverlayMaterial.SetFloat(lightEdgeFeatherId, lightEdgeFeather);
		OverlayMaterial.SetFloat(lightCornerRadiusId, lightCornerRadius);
		OverlayMaterial.SetFloat(lightFalloffStrengthId, lightFalloffStrength);
		OverlayMaterial.SetFloat(maxIlluminationId, maxIllumination);
		OverlayMaterial.SetFloat(lightCountId, lightCount);
		OverlayMaterial.SetVectorArray(lightDataId, ShaderLightData);
	}

	private static Mesh BuildOverlayMesh()
	{
		float minX = GameConfig.Grid.MinX;
		float minY = GameConfig.Grid.MinY;
		float maxX = GameConfig.Grid.MaxX + 1f;
		float maxY = GameConfig.Grid.MaxY + 1f;
		Mesh Mesh = new();
		Mesh.vertices = new Vector3[]
		{
			new(minX, minY, 0f),
			new(maxX, minY, 0f),
			new(maxX, maxY, 0f),
			new(minX, maxY, 0f),
		};
		Mesh.uv = new Vector2[]
		{
			new(0f, 0f),
			new(1f, 0f),
			new(1f, 1f),
			new(0f, 1f),
		};
		Mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
		Mesh.RecalculateNormals();
		Mesh.RecalculateBounds();
		return Mesh;
	}
}
