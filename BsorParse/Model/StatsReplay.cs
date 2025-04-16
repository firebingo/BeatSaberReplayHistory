using BeatSaberReplayHistory;
using BsorParse.Util;
using System.Buffers;
using System.Data;

namespace BsorParse.Model
{
	public enum NoteScoringType
	{
		Default,
		Ignore,
		NoScore,
		Normal,
		SliderHead,
		SliderTail,
		BurstSliderHead,
		BurstSliderElement
	}

	public class StatsReplay
	{
		public BSORInfo Info { get; set; } = new BSORInfo();
		public float LengthSeconds { get; set; }
		public int MaxCombo { get; set; }
		public int TotalScore { get; set; }
		public int MaxScore { get; set; }
		//If this run had been a full clear
		public float FullClearAccuracy { get; set; }
		public int Misses { get; set; }
		public int LeftMisses { get; set; }
		public int RightMisses { get; set; }
		public int BadCuts { get; set; }
		public int LeftBadCuts { get; set; }
		public int RightBadCuts { get; set; }
		public int BombCuts { get; set; }
		public int LeftBombCuts { get; set; }
		public int RightBombCuts { get; set; }
		public int TotalNotes { get; set; }
		public int TotalMisses
		{
			get => Misses + BadCuts + BombCuts;
		}
		public int WallHits { get; set; }
		public bool FullClear { get; set; }
		public float Accuracy { get; set; }
		public float LeftBeforeCut { get; set; }
		public float LeftAccuracy { get; set; }
		public float LeftAfterCut { get; set; }
		public float LeftPreSwing { get; set; }
		public float LeftPostSwing { get; set; }
		public float LeftTotalAccuracy { get; set; }
		public float LeftTimeDependence { get; set; }
		public float RightBeforeCut { get; set; }
		public float RightAccuracy { get; set; }
		public float RightAfterCut { get; set; }
		public float RightPreSwing { get; set; }
		public float RightPostSwing { get; set; }
		public float RightTotalAccuracy { get; set; }
		public float RightTimeDependence { get; set; }


		public StatsReplay(BSORReplay replay)
		{
			Info = replay.Info;
			MaxScore = CalcMaxScore(replay.Notes.Count);
			var baseScore = replay.Info.Score < 0 ? replay.Info.Score * -1 : replay.Info.Score;
			Accuracy = baseScore / (float)MaxScore;

			var badCuts = 0;
			var leftBadCuts = 0;
			var rightBadCuts = 0;
			var misses = 0;
			var leftMisses = 0;
			var rightMisses = 0;
			var bombCuts = 0;
			var leftBombCuts = 0;
			var rightBombCuts = 0;
			var leftCuts = ArrayPool<int>.Shared.Rent(3);
			leftCuts[0] = leftCuts[1] = leftCuts[2] = 0;
			var rightCuts = ArrayPool<int>.Shared.Rent(3);
			rightCuts[0] = rightCuts[1] = rightCuts[2] = 0;
			var leftAverageCut = ArrayPool<float>.Shared.Rent(3);
			leftAverageCut[0] = leftAverageCut[1] = leftAverageCut[2] = 0;
			float leftPreSwing = 0.0f;
			float leftTimeDependence = 0.0f;
			float leftAcc = 0.0f;
			float leftPostSwing = 0.0f;
			var rightAverageCut = ArrayPool<float>.Shared.Rent(3);
			rightAverageCut[0] = rightAverageCut[1] = rightAverageCut[2] = 0;
			float rightPreSwing = 0.0f;
			float rightTimeDependence = 0.0f;
			float rightAcc = 0.0f;
			float rightPostSwing = 0.0f;
			List<ScoreEvent> events = new List<ScoreEvent>(replay.Notes.Count + replay.Walls.Count + 1);
			try
			{
				foreach (var note in replay.Notes)
				{
					if (note.EventType == V1BSORNoteEventType.Miss)
					{
						misses++;
						if (note.CutInfo?.SaberType == 0)
							leftMisses++;
						else if (note.CutInfo?.SaberType == 1)
							rightMisses++;
					}
					else if (note.EventType == V1BSORNoteEventType.Bad)
					{
						badCuts++;
						if (note.CutInfo?.SaberType == 0)
							leftBadCuts++;
						else if (note.CutInfo?.SaberType == 1)
							rightBadCuts++;
					}
					else if (note.EventType == V1BSORNoteEventType.Bomb)
					{
						bombCuts++;
						if (note.CutInfo?.SaberType == 0)
							leftBombCuts++;
						else if (note.CutInfo?.SaberType == 1)
							rightBombCuts++;
					}

					var replayNote = ReplayNotePool.Rent();
					try
					{
						replayNote.UpdateNote(note);
						//We no longer need the original note object or notes array so free the objects.
						BSORNotePool.Return(note);
						if (note.CutInfo != null)
							BSORNoteCutPool.Return(note.CutInfo);
						if (replayNote.NoteParams.ColorType == 0)
						{
							if (replayNote.NoteParams.ScoringType != NoteScoringType.SliderTail && replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement)
							{
								leftAverageCut[0] += replayNote.BeforeCut;
								leftPreSwing += replayNote.CutInfo?.BeforeCutRating ?? 0.0f;
								leftCuts[0]++;
							}
							if (replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderHead)
							{
								leftAverageCut[1] += replayNote.Accuracy;
								leftAcc += replayNote.NoteScore;
								leftTimeDependence += Math.Abs(replayNote.CutInfo?.CutNormal.Z ?? 0.0f);
								leftCuts[1]++;
							}
							if (replayNote.NoteParams.ScoringType != NoteScoringType.SliderHead
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderHead
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement)
							{
								leftAverageCut[2] += replayNote.AfterCut;
								leftPostSwing += replayNote.CutInfo?.AfterCutRating ?? 0.0f;
								leftCuts[2]++;
							}
						}
						else
						{
							if (replayNote.NoteParams.ScoringType != NoteScoringType.SliderTail && replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement)
							{
								rightAverageCut[0] += replayNote.BeforeCut;
								rightPreSwing += replayNote.CutInfo?.BeforeCutRating ?? 0.0f;
								rightCuts[0]++;
							}
							if (replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderHead)
							{
								rightAverageCut[1] += replayNote.Accuracy;
								rightTimeDependence += Math.Abs(replayNote.CutInfo?.CutNormal.Z ?? 0.0f);
								rightAcc += replayNote.NoteScore;
								rightCuts[1]++;
							}
							if (replayNote.NoteParams.ScoringType != NoteScoringType.SliderHead
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderHead
								&& replayNote.NoteParams.ScoringType != NoteScoringType.BurstSliderElement)
							{
								rightAverageCut[2] += replayNote.AfterCut;
								rightPostSwing += replayNote.CutInfo?.AfterCutRating ?? 0.0f;
								rightCuts[2]++;
							}
						}
						var scoreEvent = ScoreEventPool.Rent();
						scoreEvent.Time = replayNote.EventTime;
						scoreEvent.ID = replayNote.NoteId;
						scoreEvent.IsBlock = replayNote.IsBlock;
						scoreEvent.Score = replayNote.NoteScore;
						scoreEvent.ScoringType = replayNote.NoteParams.ScoringType;
						scoreEvent.SpawnTime = replayNote.SpawnTime;
						events.Add(scoreEvent);
					}
					catch
					{
						throw;
					}
					finally
					{
						ReplayNotePool.Return(replayNote);
					}
				}

				foreach (var wall in replay.Walls)
				{
					var scoreEvent = ScoreEventPool.Rent();
					scoreEvent.Time = wall.Time;
					scoreEvent.ID = wall.WallId;
					scoreEvent.Score = -5;
					events.Add(scoreEvent);
				}

				if (leftCuts[0] > 0)
				{
					LeftBeforeCut = leftAverageCut[0] / leftCuts[0];
					LeftPreSwing = leftPreSwing / leftCuts[0];
				}

				if (leftCuts[1] > 0)
				{
					LeftAccuracy = leftAverageCut[1] / leftCuts[1];
					LeftTotalAccuracy = leftAcc / leftCuts[1];
					LeftTimeDependence = leftTimeDependence / leftCuts[1];
				}

				if (leftCuts[2] > 0)
				{
					LeftAfterCut = leftAverageCut[2] / leftCuts[2];
					LeftPostSwing = leftPostSwing / leftCuts[2];
				}

				if (rightCuts[0] > 0)
				{
					RightBeforeCut = rightAverageCut[0] / rightCuts[0];
					RightPreSwing = rightPreSwing / rightCuts[0];
				}

				if (rightCuts[1] > 0)
				{
					RightAccuracy = rightAverageCut[1] / rightCuts[1];
					RightTotalAccuracy = rightAcc / rightCuts[1];
					RightTimeDependence = rightTimeDependence / rightCuts[1];
				}

				if (rightCuts[2] > 0)
				{
					RightAfterCut = rightAverageCut[2] / rightCuts[2];
					RightPostSwing = rightPostSwing / rightCuts[2];
				}

				events = [.. events.OrderBy(s => s.Time)];
				var score = 0;
				int combo = 0, maxCombo = 0;
				var maxScore = 0;
				var fcScore = 0;
				float currentFcAcc = 0;
				var maxCounter = new MultiplierCounter();
				var normalCounter = new MultiplierCounter();

				for (var i = 0; i < events.Count; ++i)
				{
					var ev = events[i];
					var scoreForMaxScore = 115;
					if (ev.ScoringType == NoteScoringType.BurstSliderHead)
					{
						scoreForMaxScore = 85;
					}
					else if (ev.ScoringType == NoteScoringType.BurstSliderElement)
					{
						scoreForMaxScore = 20;
					}

					if (ev.IsBlock)
					{
						maxCounter.Increase();
						maxScore += maxCounter.Multiplier * scoreForMaxScore;
					}

					var multiplier = 1;
					if (ev.Score < 0)
					{
						normalCounter.Decrease();
						multiplier = normalCounter.Multiplier;
						combo = 0;
						if (ev.IsBlock)
						{
							fcScore += (int)Math.Round(maxCounter.Multiplier * scoreForMaxScore * currentFcAcc);
						}
					}
					else
					{
						normalCounter.Increase();
						combo++;
						multiplier = normalCounter.Multiplier;
						score += multiplier * ev.Score;
						fcScore += maxCounter.Multiplier * ev.Score;
					}

					if (combo > maxCombo)
					{
						maxCombo = combo;
					}

					ev.Multiplier = multiplier;
					ev.TotalScore = score;
					ev.MaxScore = maxScore;
					ev.Combo = combo;

					ev.Accuracy = ev.IsBlock ? (float)ev.TotalScore / maxScore : i == 0 ? 0 : events[i - 1].Accuracy;
					currentFcAcc = (float)fcScore / maxScore;
				}

				FullClearAccuracy = currentFcAcc;
				MaxCombo = maxCombo;
				TotalScore = events.Last().TotalScore;
				FullClear = replay.Walls.Count == 0 && badCuts == 0 && bombCuts == 0 && misses == 0;
				WallHits = replay.Walls.Count;
				TotalNotes = replay.Notes.Count;
				LengthSeconds = events.Last().Time;
				Misses = misses;
				LeftMisses = leftMisses;
				RightMisses = rightMisses;
				BombCuts = bombCuts;
				LeftBombCuts = leftBombCuts;
				RightBombCuts = rightBombCuts;
				BadCuts = badCuts;
				LeftBadCuts = leftBadCuts;
				RightBadCuts = rightBadCuts;
			}
			catch
			{
				throw;
			}
			finally
			{
				foreach (var ev in events)
				{
					ScoreEventPool.Return(ev);
				}
				events.Clear();
				ArrayPool<int>.Shared.Return(leftCuts);
				ArrayPool<int>.Shared.Return(rightCuts);
				ArrayPool<float>.Shared.Return(leftAverageCut);
				ArrayPool<float>.Shared.Return(rightAverageCut);
			}
		}

		private static int CalcMaxScore(int count)
		{
			int noteScore = 115;

			return count switch
			{
				// x1 (+1 note)
				<= 1 => noteScore * (0 + (count - 0) * 1),
				// x2 (+4 notes)
				<= 5 => noteScore * (1 + (count - 1) * 2),
				// x4 (+8 notes)
				<= 13 => noteScore * (9 + (count - 5) * 4),
				// x8
				_ => noteScore * (41 + (count - 13) * 8)
			};
		}

		private class MultiplierCounter
		{
			public int Multiplier { get; private set; } = 1;

			private int _multiplierIncreaseProgress;
			private int _multiplierIncreaseMaxProgress = 2;

			public void Increase()
			{
				if (Multiplier >= 8) return;

				if (_multiplierIncreaseProgress < _multiplierIncreaseMaxProgress)
				{
					++_multiplierIncreaseProgress;
				}

				if (_multiplierIncreaseProgress < _multiplierIncreaseMaxProgress) return;
				Multiplier *= 2;
				_multiplierIncreaseProgress = 0;
				_multiplierIncreaseMaxProgress = Multiplier * 2;
			}

			public void Decrease()
			{
				if (_multiplierIncreaseProgress > 0)
				{
					_multiplierIncreaseProgress = 0;
				}

				if (Multiplier <= 1) return;
				Multiplier /= 2;
				_multiplierIncreaseMaxProgress = Multiplier * 2;
			}
		}
	}

	internal class ScoreEvent
	{
		public readonly Guid Hash;
		public int Score;
		public int ID;
		public bool IsBlock;
		public float Time;
		public float SpawnTime;
		public NoteScoringType ScoringType;

		public float Multiplier;
		public int TotalScore;
		public int MaxScore;
		public float Accuracy;
		public int Combo;

		public ScoreEvent()
		{
			Hash = new Guid();
		}

		public void ClearEvent()
		{
			Score = default;
			ID = default;
			IsBlock = default;
			Time = default;
			SpawnTime = default;
			ScoringType = NoteScoringType.Default;
			Multiplier = default;
			TotalScore = default;
			MaxScore = default;
			Accuracy = default;
			Combo = default;
		}
	}

	public class ReplayNote : BSORNote
	{
		public NoteParams NoteParams { get; set; }
		public bool IsBlock { get; set; }
		public int NoteScore { get; set; }
		public int BeforeCut { get; set; }
		public int AfterCut { get; set; }
		public int Accuracy { get; set; }

		public void UpdateNote(BSORNote note)
		{
			NoteId = note.NoteId;
			CutInfo = note.CutInfo;
			EventTime = note.EventTime;
			SpawnTime = note.SpawnTime;
			EventType = note.EventType;
			NoteParams = new NoteParams(note.NoteId);

			var score = ScoreForNote(note, NoteParams);
			NoteScore = score.score;
			BeforeCut = score.before;
			AfterCut = score.after;
			Accuracy = score.acc;
			IsBlock = NoteParams.ColorType != 2;
		}

		public void ClearNote()
		{
			NoteId = default;
			CutInfo = default;
			EventTime = default;
			SpawnTime = default;
			EventType = default;
			NoteParams = default;
			NoteScore = default;
			BeforeCut = default;
			AfterCut = default;
			Accuracy = default;
			IsBlock = default;
		}

		private static (int score, int before, int after, int acc) ScoreForNote(BSORNote note, NoteParams nparams)
		{
			if (note.EventType != V1BSORNoteEventType.Good)
				return note.EventType switch
				{
					V1BSORNoteEventType.Bad => (-2, 0, 0, 0),
					V1BSORNoteEventType.Miss => (-3, 0, 0, 0),
					V1BSORNoteEventType.Bomb => (-4, 0, 0, 0),
					_ => (-1, 0, 0, 0)
				};
			//This shouldnt happen but just in case.
			if (note.CutInfo == null)
				return (0, 0, 0, 0);

			var (before, after, acc) = CutScoresForNote(note, nparams);
			return (before + after + acc, before, after, acc);
		}

		private static (int before, int after, int acc) CutScoresForNote(BSORNote note, NoteParams nparams)
		{
			var cut = note.CutInfo!;
			double beforeCutRawScore = 0;
			if (nparams.ScoringType != NoteScoringType.BurstSliderElement)
			{
				beforeCutRawScore = nparams.ScoringType == NoteScoringType.SliderTail ? 70 : Math.Clamp(Math.Round(70 * cut.BeforeCutRating), 0, 70);
			}
			double afterCutRawScore = 0;
			if (nparams.ScoringType != NoteScoringType.BurstSliderElement)
			{
				if (nparams.ScoringType == NoteScoringType.BurstSliderHead)
				{
					afterCutRawScore = 0;
				}
				else if (nparams.ScoringType == NoteScoringType.SliderHead)
				{
					afterCutRawScore = 30;
				}
				else
				{
					afterCutRawScore = Math.Clamp(Math.Round(30 * cut.AfterCutRating), 0, 30);
				}
			}
			double cutDistanceRawScore;
			if (nparams.ScoringType == NoteScoringType.BurstSliderElement)
			{
				cutDistanceRawScore = 20;
			}
			else
			{
				double num = 1 - Math.Clamp(cut.CutDistanceToCenter / 0.3f, 0, 1);
				cutDistanceRawScore = Math.Round(15 * num);
			}

			return ((int)beforeCutRawScore, (int)afterCutRawScore, (int)cutDistanceRawScore);
		}
	}

	public readonly struct NoteParams
	{
		public readonly NoteScoringType ScoringType;
		public readonly int LineIndex;
		public readonly int NoteLineLayer;
		public readonly int ColorType;
		public readonly int CutDirection;

		public NoteParams(int noteId)
		{
			var id = noteId;
			if (id < 100000)
			{
				ScoringType = (NoteScoringType)(id / 10000);
				id -= (int)ScoringType * 10000;

				LineIndex = id / 1000;
				id -= LineIndex * 1000;

				NoteLineLayer = id / 100;
				id -= NoteLineLayer * 100;

				ColorType = id / 10;
				CutDirection = id - ColorType * 10;
			}
			else
			{
				ScoringType = (NoteScoringType)(id / 10000000);
				id -= (int)ScoringType * 10000000;

				LineIndex = id / 1000000;
				id -= LineIndex * 1000000;

				NoteLineLayer = id / 100000;
				id -= NoteLineLayer * 100000;

				ColorType = id / 10;
				CutDirection = id - ColorType * 10;
			}
		}
	}
}
