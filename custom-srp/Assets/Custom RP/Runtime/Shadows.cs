using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {

	const string bufferName = "Shadows";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;

	CullingResults cullingResults;

	ShadowSettings settings;

	const int maxShadowedDirectionalLightCount = 1;
	int shadowedDirLightCount;
	static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

	struct ShadowedDirectionalLight {
		public int visibleLightIndex;
	}

	ShadowedDirectionalLight[] ShadowedDirectionalLights =
		new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

	public void ReserveDirectionalShadows (Light light, int visibleLightIndex) {
		if (shadowedDirLightCount < maxShadowedDirectionalLightCount  &&
			light.shadows != LightShadows.None && light.shadowStrength > 0f &&
			cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			ShadowedDirectionalLights[shadowedDirLightCount++] =
				new ShadowedDirectionalLight {
					visibleLightIndex = visibleLightIndex
				};
		}
	}

	public void Render () {
		if (shadowedDirLightCount > 0) {
			RenderDirectionalShadows();
		}
		else {
			buffer.GetTemporaryRT(
				dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
			);
		}
	}

	void RenderDirectionalShadows () {
		int atlasSize = (int)settings.directional.atlasSize;
		buffer.GetTemporaryRT(
			dirShadowAtlasId, atlasSize, atlasSize,
			32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
		);

		buffer.SetRenderTarget(
			dirShadowAtlasId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		buffer.ClearRenderTarget(true, false, Color.clear);
		ExecuteBuffer();
	}

	public void Setup (
		ScriptableRenderContext context, CullingResults cullingResults,
		ShadowSettings settings
	) {
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;
		shadowedDirLightCount = 0;
	}

	void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	public void Cleanup () {
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}
}