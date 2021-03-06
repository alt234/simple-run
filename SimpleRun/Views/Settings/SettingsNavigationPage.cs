﻿using System;
using System.Net;
using System.Threading;
using System.Reactive.Linq;
using Xamarin.Forms;

namespace SimpleRun.Views.Settings
{
	public class SettingsNavigationPage : NavigationPage
	{
		public SettingsNavigationPage() : base(new SettingsListPage())
		{
			if (Device.OS == TargetPlatform.iOS)
				Tint = App.HeaderTint;

			Icon = "settings@2x.png";
			Title = "Settings";
		}
	}
}

