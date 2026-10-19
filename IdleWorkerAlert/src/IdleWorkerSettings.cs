using System;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;
using UnityEngine;

namespace Local.IdleWorkerAlert
{
    public sealed class IdleWorkerSettings : ModSettingsOwner
    {
        public const float DefaultGracePeriodInHours = 1f;
        public const float MinimumGracePeriodInHours = 0f;
        public const float MaximumGracePeriodInHours = 12f;

        private const string ModIdValue = "gem0303.IdleWorkerAlert";
        private const string HeaderLocKeyValue = "Local.IdleWorkerAlert.Settings.Header";
        private const string GracePeriodLocKey = "Local.IdleWorkerAlert.Settings.GracePeriod";
        private const string GracePeriodTooltipLocKey = "Local.IdleWorkerAlert.Settings.GracePeriodTooltip";

        public IdleWorkerSettings(
            ISettings settings,
            ModSettingsOwnerRegistry modSettingsOwnerRegistry,
            ModRepository modRepository)
            : base(settings, modSettingsOwnerRegistry, modRepository)
        {
            GracePeriodInHours = new BoundedFloatModSetting(
                DefaultGracePeriodInHours,
                MinimumGracePeriodInHours,
                MaximumGracePeriodInHours,
                ModSettingDescriptor.CreateLocalized(GracePeriodLocKey)
                    .SetLocalizedTooltip(GracePeriodTooltipLocKey));
        }

        public ModSetting<float> GracePeriodInHours { get; private set; }

        public override string HeaderLocKey
        {
            get { return HeaderLocKeyValue; }
        }

        public override ModSettingsContext ChangeableOn
        {
            get { return ModSettingsContext.All; }
        }

        protected override string ModId
        {
            get { return ModIdValue; }
        }

        private sealed class BoundedFloatModSetting : ModSetting<float>
        {
            private readonly float _minimumValue;
            private readonly float _maximumValue;

            public BoundedFloatModSetting(
                float defaultValue,
                float minimumValue,
                float maximumValue,
                ModSettingDescriptor descriptor)
                : base(defaultValue, descriptor)
            {
                _minimumValue = minimumValue;
                _maximumValue = maximumValue;
            }

            public override void SetValue(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    value = DefaultValue;
                }

                base.SetValue(Mathf.Clamp(value, _minimumValue, _maximumValue));
            }
        }
    }
}
