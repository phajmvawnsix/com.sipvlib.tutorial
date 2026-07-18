using System;

namespace SiPVLib.Tutorial
{
    /// <summary>
    /// Decorates a <see cref="SiPVLib.Tutorial.Config.TutorialNode"/> subclass with a
    /// display name and hex color used by the Tutorial Graph editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TutorialNodeLabelAttribute : Attribute
    {
        public string Name     { get; }
        public string HexColor { get; }

        public TutorialNodeLabelAttribute(string name, string hexColor = "#4A90D9")
        {
            Name     = name;
            HexColor = hexColor;
        }
    }
}

