// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPresetsViewModel.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Defines the IPresetsViewModel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HandBrakeWPF.Services.Presets.Model;

namespace HandBrakeWPF.ViewModels.Interfaces
{
    /// <summary>
    /// The Presets View Model Interface
    /// </summary>
    public interface IPresetsViewModel
    {
        void PresetSelect(object tag);
    }
}
