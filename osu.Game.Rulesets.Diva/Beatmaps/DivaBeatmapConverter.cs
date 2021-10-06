// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Diva.Objects;
using osuTK;
using System;
using System.Threading;

namespace osu.Game.Rulesets.Diva.Beatmaps
{
    public class DivaBeatmapConverter : BeatmapConverter<DivaHitObject>
    {
        //todo:
        //make single position bursts to a line pattern
        //every approach piece of a combo will come from one direction
        //create patterns of same button

        public int TargetButtons;
        public bool AllowDoubles = true;

        private int prevAction = 0;
        private Vector2 prevObjectPos = Vector2.Zero;
        private Vector2 originalPos = Vector2.Zero;
        private Vector2 prevPos = Vector2.Zero;
        //these variables were at the end of the class, such heresy had i done

        private const float approach_piece_distance = 1200;

        public DivaBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
            this.TargetButtons = beatmap.BeatmapInfo.BaseDifficulty.OverallDifficulty switch
            {
                >= 6.0f => 4,
                >= 4.5f => 3,
                >= 2f => 2,
                _ => 1,
            };

            //Console.WriteLine(this.TargetButtons);
        }

        public override bool CanConvert() => Beatmap.HitObjects.All(h => h is IHasPosition);

        protected override IEnumerable<DivaHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            //not sure if handling the cancellation is needed, as offical modes doesnt handle *scratches my head* or even its possible
            var pos = validatePos((original as IHasPosition)?.Position ?? Vector2.Zero);
            var comboData = original as IHasCombo;

            //currently press presses are placed in place of sliders as placeholder, but arcade slider are better suited for these
            //another option would be long sliders: arcade sliders, short sliders: doubles
            if (AllowDoubles && original is IHasPathWithRepeats)
            {
                yield return new DoublePressButton
                {
                    Samples = original.Samples,
                    StartTime = original.StartTime,
                    Position = pos,
                    ValidAction = ValidAction(comboData?.NewCombo ?? false, original.StartTime),
                    DoubleAction = ArrowAction(prevAction),
                    ApproachPieceOriginPosition = GetApproachPieceOriginPos(pos),
                };
            }
            else
            {
                yield return new DivaHitObject
                {
                    Samples = original.Samples,
                    StartTime = original.StartTime,
                    Position = pos,
                    ValidAction = ValidAction(comboData?.NewCombo ?? false, original.StartTime),
                    ApproachPieceOriginPosition = GetApproachPieceOriginPos(pos),
                };
            }

        }

        private static DivaAction ArrowAction(int ac) => ac switch
        {
            0 => DivaAction.Right,
            1 => DivaAction.Down,
            2 => DivaAction.Left,
            _ => DivaAction.Up
        };

        private static DivaAction ShapeAction(int ac) => ac switch
        {
            0 => DivaAction.Circle,
            1 => DivaAction.Cross,
            2 => DivaAction.Square,
            _ => DivaAction.Triangle
        };


        private Vector2 validatePos(Vector2 pos) {


            if (pos == originalPos) {
                pos.X = prevPos.X + 10;
                pos.Y = prevPos.Y + 10;
                prevPos = pos;
            } else {
                originalPos = pos;
                prevPos = pos;
            }
            return pos;
        }




        private DivaAction ValidAction(bool newCombo, double startTime)
        {
            if (newCombo)
            {

                var noteType = (int)Math.Floor(startTime * Math.PI * 1000) % this.TargetButtons;


                if (noteType == prevAction)
                {
                    noteType = (noteType + 1) % this.TargetButtons;
                }

                prevAction = noteType;
                return ShapeAction(noteType);
            }
            else
            {
                return ShapeAction(prevAction);
            };
        }

        private Vector2 GetApproachPieceOriginPos(Vector2 currentObjectPos)
        {
            var dir = (prevObjectPos - currentObjectPos);
            prevObjectPos = currentObjectPos;

            if (dir == Vector2.Zero)
                return new Vector2(approach_piece_distance);

            return dir.Normalized() * approach_piece_distance;
        }
    }
}
