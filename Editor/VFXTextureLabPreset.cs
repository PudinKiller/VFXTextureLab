#if UNITY_EDITOR
using UnityEngine;

namespace PudinKiller.VFXTextureLab
{
    [CreateAssetMenu(fileName = "VFXTextureLabPreset", menuName = "Pudin Killer/VFX Texture Lab Preset")]
    public class VFXTextureLabPreset : ScriptableObject
    {
        public VFXTextureLabSettings settings = new VFXTextureLabSettings();
    }
}
#endif
