using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Input.Data
{
    public readonly struct InputInfo
    {
        [NotNull] public readonly string deviceTypeName;
        [NotNull] public readonly Type deviceType;
        [NotNull] public readonly string pathPart;
        [CanBeNull] public readonly string displayName;
        [CanBeNull] public readonly string shortName;

        public InputInfo([NotNull] Type deviceType, [NotNull] string pathPart, [CanBeNull] string displayName, [CanBeNull] string shortName)
        {
            this.deviceType = deviceType;
            this.pathPart = pathPart;
            this.displayName = displayName;
            this.shortName = shortName;
            deviceTypeName = deviceType.Name.Replace("State", "");
            
            // Ensure path is properly formatted
            if(!this.pathPart.StartsWith('/'))
                this.pathPart = '/' + this.pathPart;
        }

        [CanBeNull] public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayName)) return displayName;
                if (!string.IsNullOrEmpty(shortName)) return shortName;
                return null;
            }
        }
            
        [CanBeNull] public string ShortName
        {
            get
            {
                if (!string.IsNullOrEmpty(shortName)) return shortName;
                if (!string.IsNullOrEmpty(displayName)) return displayName;
                return null;
            }
        }
    }
}