using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliIsland
{
    [DisallowMultipleComponent]
    public sealed class DiwaliIslandRendering : MonoBehaviour
    {
        public RenderPipelineAsset islandPipeline;
        RenderPipelineAsset previousPipeline;
        bool applied;

        void OnEnable()
        {
            if (islandPipeline == null || QualitySettings.renderPipeline == islandPipeline) return;
            previousPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = islandPipeline;
            applied = true;
        }

        void OnDisable()
        {
            if (applied && QualitySettings.renderPipeline == islandPipeline)
                QualitySettings.renderPipeline = previousPipeline;
            applied = false;
        }
    }
}
