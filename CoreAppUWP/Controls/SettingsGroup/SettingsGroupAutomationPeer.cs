using Microsoft.UI.Xaml.Automation.Peers;

namespace CoreAppUWP.Controls
{
    /// <param name="owner">SettingsGroup</param>
    public class SettingsGroupAutomationPeer(SettingsGroup owner) : ItemsControlAutomationPeer(owner)
    {
        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            SettingsGroup selectedSettingsGroup = (SettingsGroup)Owner;
            return selectedSettingsGroup.Header is string header ? header : selectedSettingsGroup.Name;
        }
    }
}
