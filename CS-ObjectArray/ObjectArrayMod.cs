using ICities;

namespace ObjectArray
{
    public class ObjectArray : IUserMod
    {
        public string Name => "Object Array";
        public string Description => "How to explain lol";

        public static float selectionRadius = 10f;
        public static byte selectionOpacity = 127;

        public void OnSettingsUI (UIHelperBase helperBase){
            helperBase.AddSlider("Point selection radius", 2f, 50f, 0.2f, selectionRadius, (b) =>
            {
                selectionRadius = b;
            });

            helperBase.AddSlider("Point selection opacity", 0f, 255f, 1f, selectionOpacity, (b) =>
            {
                selectionOpacity = (byte) b;
            });
        }
    }
}
