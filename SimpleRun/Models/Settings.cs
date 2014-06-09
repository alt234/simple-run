﻿using System;
using System.Threading.Tasks;
using SQLite.Net.Attributes;
using SimpleRun.DataAccess;

namespace SimpleRun.Models
{
	public class Settings
	{
		[PrimaryKey, AutoIncrement]
		public int ID { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }

		public static string GetValueForKey(string key)
		{
			Settings setting = null;

			lock (Database.Main) {
				setting = Database.Main.Table<Settings>().Where(s => s.Key == key).FirstOrDefault();
			}

			if (setting == null)
				return string.Empty;

			return setting.Value;
		}

		public static void CreateKeyValue(string key, string value)
		{
			var setting = new Settings {
				Key = key,
				Value = value
			};

			lock (Database.Main) {
				Database.Main.Insert(setting);
			}
		}

		[Ignore]
		public static MeasurementType MeasurementType {
			get {
				var typeString = GetValueForKey("MeasurementType");

				var theEnum = MeasurementType.None;
				Enum.TryParse<MeasurementType>(typeString, out theEnum);
				return theEnum;
			}
			set {
				lock (Database.Main) {
					Database.Main.RunInTransaction(() => {
						var setting = Database.Main.Table<Settings>().Where(s => s.Key == "MeasurementType").FirstOrDefault();
						setting.Value = value.ToString();
						Database.Main.Update(setting);
					});
				}
			}
		}
	}
}
