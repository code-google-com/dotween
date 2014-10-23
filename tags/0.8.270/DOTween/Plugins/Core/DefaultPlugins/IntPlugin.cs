﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2014/07/10 14:15
// 
// License Copyright (c) Daniele Giardini.
// This work is subject to the terms at http://dotween.demigiant.com/license.php

using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Core.DefaultPlugins.Options;

#pragma warning disable 1591
namespace DG.Tweening.Plugins.Core.DefaultPlugins
{
    public class IntPlugin : ABSTweenPlugin<int, int, NoOptions>
    {
        public override void Reset(TweenerCore<int, int, NoOptions> t) { }

        public override int ConvertToStartValue(TweenerCore<int, int, NoOptions> t, int value)
        {
            return value;
        }

        public override void SetRelativeEndValue(TweenerCore<int, int, NoOptions> t)
        {
            t.endValue += t.startValue;
        }

        public override void SetChangeValue(TweenerCore<int, int, NoOptions> t)
        {
            t.changeValue = t.endValue - t.startValue;
        }

        public override float GetSpeedBasedDuration(NoOptions options, float unitsXSecond, int changeValue)
        {
            float res = changeValue / unitsXSecond;
            if (res < 0) res = -res;
            return res;
        }

        public override void EvaluateAndApply(NoOptions options, Tween t, bool isRelative, DOGetter<int> getter, DOSetter<int> setter, float elapsed, int startValue, int changeValue, float duration)
        {
            if (t.loopType == LoopType.Incremental) startValue += changeValue * (t.isComplete ? t.completedLoops - 1 : t.completedLoops);

            setter((int)Math.Round(EaseManager.Evaluate(t, elapsed, startValue, changeValue, duration, t.easeOvershootOrAmplitude, t.easePeriod)));
        }
    }
}