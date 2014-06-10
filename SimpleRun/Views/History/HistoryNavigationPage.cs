﻿using System;
using System.Net;
using System.Threading;
using System.Reactive.Linq;
using Xamarin.Forms;

namespace SimpleRun.Views.History
{
	public class HistoryNavigationPage : NavigationPage
	{
		public HistoryNavigationPage() : base(new HistoryListPage())
		{
			Tint = App.HeaderTint;
			Icon = "book@2x.png";
			Title = "History";
		}
	}
}
