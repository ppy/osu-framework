// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    internal interface IDropdown
    {
        /// <summary>
        /// An event that occurs when the menu state changes.
        /// </summary>
        event Action<MenuState> MenuStateChanged;

        /// <summary>
        /// Whether the dropdown is currently enabled.
        /// </summary>
        IBindable<bool> Enabled { get; }

        /// <summary>
        /// The current menu state.
        /// </summary>
        MenuState MenuState { get; }

        /// <summary>
        /// Toggles the menu.
        /// </summary>
        void ToggleMenu();

        /// <summary>
        /// Opens the menu.
        /// </summary>
        void OpenMenu();

        /// <summary>
        /// Closes the menu.
        /// </summary>
        void CloseMenu();

        /// <summary>
        /// Commits the current pre-selected value.
        /// </summary>
        void CommitPreselection();

        /// <summary>
        /// Triggers focus contention on the parenting <see cref="IFocusManager"/>.
        /// </summary>
        /// <remarks>
        /// Focus management is isolated by the <see cref="Dropdown{T}"/>. This invokes the method on the parenting <see cref="IFocusManager"/> un-interrupted.
        /// </remarks>
        void TriggerFocusContention(Drawable? triggerSource);

        /// <summary>
        /// Triggers a change of focus on the parenting <see cref="IFocusManager"/>.
        /// </summary>
        /// <remarks>
        /// Focus management is isolated by the <see cref="Dropdown{T}"/>. This invokes the method on the parenting <see cref="IFocusManager"/> un-interrupted.
        /// </remarks>
        bool ChangeFocus(Drawable? potentialFocusTarget);
    }
}
