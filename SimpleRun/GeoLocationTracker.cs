﻿using System;
using System.Linq;
using System.Collections.Generic;
using Xamarin.Geolocation;
using SimpleRun.Models;
using SimpleRun.Extensions;

#if __ANDROID__
using Android.Content;
#endif

namespace SimpleRun
{
	public class GeoLocationTracker
	{
		const int positionHistorySize = 5;
		const int minPositionsNeededToUpdateStats = 3;
		const double maxHorizontalAccuracy = 40.0f;

		int positionCounter = 0;

		Geolocator geolocator;

		TimeSpan statsCalculationInterval;
		TimeSpan validLocationHistoryDeltaInterval;

		DateTimeOffset previousDistanceTime;
		DateTimeOffset startTime;

		Position previousPosition;
		List<Position> positionHistory;
		List<Position> routePositions;
		List<RunPosition> totalPositionHistory;
		List<double> totalAveragePaceHistory;

		public double CurrentDistance { get; set; }
		public string CurrentDistanceString {
			get {
				if (CurrentDistance == 0)
					return "0.00 " + UnitUtility.DistanceUnitString;

				return string.Format("{0} {1}", Math.Round(CurrentDistance * UnitUtility.ConversionValue, 2).ToString("F"), UnitUtility.DistanceUnitString);
			}
		}
		public double CurrentPace { get; set; }
		public string CurrentPaceString {
			get {
				if (CurrentPace == 0) 
					return "00:00 per " + UnitUtility.DistanceUnitString;

				double minutesPerUnit = Math.Round(1 / (CurrentPace * UnitUtility.ConversionValue * 60.0), 2);
				return string.Format("{0} per {1}", TimeSpan.FromMinutes(minutesPerUnit).ToString(@"mm\:ss"), UnitUtility.DistanceUnitString);
			}
		}
		public double AveragePace { 
			get { 
				if (totalAveragePaceHistory.Count == 0)
					return 0;

				return totalAveragePaceHistory.Average();
			}
		}

		void Init()
		{
			CurrentDistance = 0;
			CurrentPace = 0;
			positionHistory = new List<Position>();
			routePositions = new List<Position>();
			totalPositionHistory = new List<RunPosition>();
			totalAveragePaceHistory = new List<double>();
			previousPosition = null;
			statsCalculationInterval = new TimeSpan(0, 0, 1);
			validLocationHistoryDeltaInterval = new TimeSpan(0, 0, 1);
		}
#if __ANDROID__
		public void BeginTrackingLocation(Context context)
#else
		public void BeginTrackingLocation()
#endif
		{
			Init();

#if __ANDROID__
			geolocator = new Geolocator(context) { DesiredAccuracy = 1 };
#else
			geolocator = new Geolocator() { DesiredAccuracy = 1 };
#endif

			startTime = DateTime.Now;

			geolocator.PositionChanged += delegate(object sender, PositionEventArgs e) {
				if (e.Position.Accuracy == 0.0f || e.Position.Accuracy > maxHorizontalAccuracy)
					return;

				positionHistory.Add(e.Position);

				if (previousPosition == null)
					previousDistanceTime = startTime;

				if (positionHistory.Count > positionHistorySize)
					positionHistory.RemoveAt(0);

				var canUpdateStats = positionHistory.Count >= minPositionsNeededToUpdateStats;

				if (e.Position.Timestamp - previousDistanceTime <= statsCalculationInterval)
					return;

				previousDistanceTime = e.Position.Timestamp;

				Position bestPosition = null;
				var bestAccuracy = maxHorizontalAccuracy;

				foreach (var position in positionHistory) {
					if (DateTimeOffset.Now - position.Timestamp <= validLocationHistoryDeltaInterval && position.Accuracy < bestAccuracy && position != previousPosition) {
						bestAccuracy = position.Accuracy;
						bestPosition = position;
					}
				}

				if (bestPosition == null) bestPosition = e.Position;

				if (canUpdateStats) {
					if (previousPosition != null) {
						CurrentDistance += DistanceInMeters(bestPosition, previousPosition);

						totalAveragePaceHistory.Add(bestPosition.Speed);
						CurrentPace = bestPosition.Speed;
					}
				}

				routePositions.Add(bestPosition);
				positionCounter++;

				// Only insert every fifth point to minimize the amount of data being stored.
				if (totalPositionHistory.Count == 0 || positionCounter % 5 == 0) {
					totalPositionHistory.Add(new RunPosition {
						Speed = bestPosition.Speed,
						Latitude = bestPosition.Latitude,
						Longitude = bestPosition.Longitude,
						PositionCaptureTime = bestPosition.Timestamp.DateTime
					});
				}

				previousPosition = bestPosition;
			};

			geolocator.PositionError += delegate(object sender, PositionErrorEventArgs e) {
				Console.WriteLine("Location Manager Failed: " + e.Error);
			};

			geolocator.StartListening(minTime: 1000, minDistance: 1, includeHeading: false);
		}

		public void StopTrackingLocation()
		{
			geolocator.StopListening();

			TimeSpan totalRunTime = DateTimeOffset.Now - startTime;

			var newRun = new Run {
				RunDate = DateTime.Now,
				DistanceInMeters = CurrentDistance,
				AveragePaceInMetersPerSecond = AveragePace,
				DurationInSeconds = Convert.ToInt32(Math.Round(totalRunTime.TotalSeconds, 0)),
			};

			newRun.Create(totalPositionHistory);

			Init();
		}

		/// <summary>
		/// Used for calculating distance in meters between two points. 
		/// Code pulled from here: http://slodge.blogspot.com/2012/04/calculating-distance-between-latlng.html
		/// Attributed originally to here: http://www.movable-type.co.uk/scripts/latlong.html 
		/// </summary>
		/// <returns>The distance in meters.</returns>
		/// <param name="position1">The furthest along position</param>
		/// <param name="position2">The position to determine distance from.</param>
		public double DistanceInMeters(Position recentPosition, Position previousPosition)
		{
			var lat1 = recentPosition.Latitude;
			var lon1 = recentPosition.Longitude;

			var lat2 = previousPosition.Latitude;
			var lon2 = previousPosition.Longitude;

			if (lat1 == lat2 && lon1 == lon2)
				return 0.0;

			var theta = lon1 - lon2;

			var distance = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) +
				Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) *
				Math.Cos(deg2rad(theta));

			distance = Math.Acos(distance);
			if (double.IsNaN(distance))
				return 0.0;

			distance = rad2deg(distance);
			distance = distance * 60.0 * 1.1515 * 1609.344;

			return distance;
		}

		double deg2rad(double deg)
		{
			return (deg * Math.PI / 180.0);
		}

		double rad2deg(double rad)
		{
			return (rad / Math.PI * 180.0);
		}
	}
}
