using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace benjohnson
{
    /// <summary>
    /// Flashes a UI <see cref="Graphic"/> to a solid colour (white by default) for a moment, e.g. on a hit, then snaps
    /// or fades it back.
    /// Gives the Graphic its own material from <see cref="flashShader"/>, which must have <c>_FlashColor</c> and
    /// <c>_FlashAmount</c> properties. Any material already on the Graphic is replaced while this component exists.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class GraphicFlash : MonoBehaviour
    {
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [Tooltip("A UI shader with _FlashColor and _FlashAmount, such as GGO2026/UI Flash. Referencing it here also includes it in builds.")]
        [SerializeField] private Shader flashShader;

        [Tooltip("The colour the Graphic turns at the start of a flash. Its alpha is how strongly it covers the art.")]
        [SerializeField] private Color flashColour = Color.white;

        [Tooltip("Seconds the flash stays at full strength. Always at least one frame.")]
        [Min(0f)]
        [SerializeField] private float holdDuration = 0.08f;

        [Tooltip("Seconds to fade back to normal after the hold. 0 snaps straight back, as in most pixel art games.")]
        [Min(0f)]
        [FormerlySerializedAs("duration")]
        [SerializeField] private float fadeDuration;

        private Graphic graphic;
        private Material flashMaterial;
        private Material originalMaterial;
        private Coroutine fade;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            if (flashShader == null)
            {
                Debug.LogError($"GraphicFlash on '{name}' has no flash shader assigned, so it won't flash.", this);
                return;
            }

            // Graphic.material returns the default UI material when none is set; remember "none" as null.
            originalMaterial = graphic.material == graphic.defaultMaterial ? null : graphic.material;
            if (originalMaterial != null)
                Debug.LogWarning($"GraphicFlash on '{name}' replaces its material '{originalMaterial.name}'.", this);

            flashMaterial = new Material(flashShader) { name = $"{flashShader.name} ({name})", hideFlags = HideFlags.DontSave };
            flashMaterial.SetFloat(FlashAmountId, 0f);
            graphic.material = flashMaterial;
        }

        private void OnDisable()
        {
            // A coroutine stops with the object, so clear the flash rather than leave it stuck part way.
            fade = null;
            SetAmount(0f);
        }

        private void OnDestroy()
        {
            if (flashMaterial == null) return;
            if (graphic != null) graphic.material = originalMaterial;
            Destroy(flashMaterial);
        }

        /// <summary>Starts a flash. One already playing restarts at full strength.</summary>
        public void Flash()
        {
            if (flashMaterial == null || !isActiveAndEnabled) return;

            flashMaterial.SetColor(FlashColorId, flashColour);
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade());
        }

        private IEnumerator Fade()
        {
            // The first frame always shows the full flash, however short the hold.
            SetAmount(1f);
            float elapsed = 0f;
            do
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            while (elapsed < holdDuration);

            for (elapsed = 0f; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                SetAmount(1f - elapsed / fadeDuration);
                yield return null;
            }

            SetAmount(0f);
            fade = null;
        }

        private void SetAmount(float amount)
        {
            if (flashMaterial != null) flashMaterial.SetFloat(FlashAmountId, amount);
        }
    }
}
