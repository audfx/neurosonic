using theori.Charting;

namespace NeuroSonic.Charting
{
    [EntityType("Button")]
    public sealed class ButtonEntity : Entity
    {
        public ButtonEntity Head => FirstConnectedOf<ButtonEntity>();
        public ButtonEntity Tail => LastConnectedOf<ButtonEntity>();

        public bool IsChip => IsInstant;
        public bool IsHold => !IsInstant;

        public bool HasSample => Sample != null;

        [TheoriIgnoreDefault]
        [TheoriProperty("sample")]
        public string Sample { get; set; }

        [TheoriIgnoreDefault]
        [TheoriProperty("sampleVolume")]
        public float SampleVolume { get; set; }
    }
}
