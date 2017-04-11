﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Input;
using OpenTK;
using System.Diagnostics;

namespace osu.Framework.Graphics.UserInterface
{
	public abstract class SliderBar<T> : Container, IHasCurrentValue<T>
		where T : struct
	{
		/// <summary>
		/// Range padding reduces the range of movement a slider bar is allowed to have
		/// while still receiving input in the padded region. This behavior is necessary
		/// for finite-sized nubs and can not be achieved (currently) by existing
		/// scene graph padding / margin functionality.
		/// </summary>
		public float RangePadding;

		public float UsableWidth => DrawWidth - 2 * RangePadding;

		private float keyboardStep;

		public float KeyboardStep
		{
			get { return keyboardStep; }
			set
			{
				keyboardStep = value;
				stepInitialized = true;
			}
		}

		private bool stepInitialized;

		private readonly BindableNumber<T> current;

		public Bindable<T> Current => current;

		private bool PlaySound;

		protected SliderBar()
		{
			if (typeof(T) == typeof(int))
				current = new BindableInt() as BindableNumber<T>;
			else if (typeof(T) == typeof(long))
				current = new BindableLong() as BindableNumber<T>;
			else if (typeof(T) == typeof(double))
				current = new BindableDouble() as BindableNumber<T>;

			if (current == null) throw new NotImplementedException($"We don't support the generic type of {nameof(BindableNumber<T>)}.");

			current.ValueChanged += v => UpdateValue(NormalizedValue);
		}

		protected float NormalizedValue
		{
			get
			{
				if (Current == null)
					return 0;
				var min = Convert.ToSingle(current.MinValue);
				var max = Convert.ToSingle(current.MaxValue);
				var val = Convert.ToSingle(current.Value);
				return (val - min) / (max - min);
			}
		}

		protected abstract void UpdateValue(float value);

		protected override void Dispose(bool isDisposing)
		{
			if (Current != null)
				Current.ValueChanged -= bindableValueChanged;
			base.Dispose(isDisposing);
		}

		protected override void LoadComplete()
		{
			base.LoadComplete();
			PlaySound = false;
			UpdateValue(NormalizedValue);
			PlaySound = true;
		}

		protected override bool OnClick(InputState state)
		{
			handleMouseInput(state);
			return true;
		}

		protected override bool OnDrag(InputState state)
		{
			handleMouseInput(state);
			return true;
		}

		protected override bool OnDragStart(InputState state)
		{
			Trace.Assert(state.Mouse.PositionMouseDown.HasValue,
				$@"Can not start a {nameof(SliderBar<T>)} drag without knowing the mouse down position.");

			Vector2 posDiff = state.Mouse.PositionMouseDown.Value - state.Mouse.Position;

			return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
		}

		protected override bool OnDragEnd(InputState state) => true;

		protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
		{
			if (!Hovering)
				return false;
			if (!stepInitialized)
				KeyboardStep = (Convert.ToSingle(current.MaxValue) - Convert.ToSingle(current.MinValue)) / 20;
			var step = KeyboardStep;
			if (current.IsInteger)
				step = (float)Math.Ceiling(step);
			switch (args.Key)
			{
				case Key.Right:
					current.Add(step);
					return true;
				case Key.Left:
					current.Add(-step);
					return true;
				default:
					return false;
			}
		}

		private void bindableValueChanged(T newValue) => UpdateValue(NormalizedValue);

		private void handleMouseInput(InputState state)
		{
			var xPosition = ToLocalSpace(state.Mouse.NativeState.Position).X - RangePadding;
			current.SetProportional(xPosition / UsableWidth);
		}
	}
}
