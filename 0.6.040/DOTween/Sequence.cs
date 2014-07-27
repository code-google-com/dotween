﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2014/07/15 17:50
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System.Collections.Generic;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using UnityEngine;

namespace DG.Tweening
{
    /// <summary>
    /// Controls a collection of tweens
    /// </summary>
    public sealed class Sequence : Tween
    {
        // SETUP DATA ////////////////////////////////////////////////

        internal readonly List<Tween> sequencedTweens = new List<Tween>(); // Only Tweens (used for despawning)
        readonly List<ABSSequentiable> _sequencedObjs = new List<ABSSequentiable>(); // Tweens plus SequenceCallbacks

        internal Sequence()
        {
            tweenType = TweenType.Sequence;
            Reset();
        }

        // ===================================================================================
        // CREATION METHODS ------------------------------------------------------------------

        internal static Sequence DoPrepend(Sequence inSequence, Tween t)
        {
            inSequence.duration += t.duration;
            int len = inSequence._sequencedObjs.Count;
            for (int i = 0; i < len; ++i) {
                ABSSequentiable sequentiable = inSequence._sequencedObjs[i];
                sequentiable.sequencedPosition += t.duration;
                sequentiable.sequencedEndPosition += t.duration;
            }

            return DoInsert(inSequence, t, 0);
        }

        internal static Sequence DoInsert(Sequence inSequence, Tween t, float atPosition)
        {
            TweenManager.AddActiveTweenToSequence(t);

            t.isSequenced = t.creationLocked = true;
            if (t.loops == -1) t.loops = 1;
            t.autoKill = false;
            t.delay = t.elapsedDelay = 0;
            t.delayComplete = true;
            t.sequencedPosition = atPosition;
            t.sequencedEndPosition = t.sequencedPosition + (t.duration * t.loops);

            if (t.sequencedEndPosition > inSequence.duration) inSequence.duration = t.sequencedEndPosition;
            inSequence._sequencedObjs.Add(t);
            inSequence.sequencedTweens.Add(t);

            return inSequence;
        }

        internal static Sequence DoAppendInterval(Sequence inSequence, float interval)
        {
            inSequence.duration += interval;
            return inSequence;
        }

        internal static Sequence DoPrependInterval(Sequence inSequence, float interval)
        {
            inSequence.duration += interval;
            int len = inSequence._sequencedObjs.Count;
            for (int i = 0; i < len; ++i) {
                ABSSequentiable sequentiable = inSequence._sequencedObjs[i];
                sequentiable.sequencedPosition += interval;
                sequentiable.sequencedEndPosition += interval;
            }

            return inSequence;
        }

        internal static Sequence DoInsertCallback(Sequence inSequence, TweenCallback callback, float atPosition)
        {
            SequenceCallback c = new SequenceCallback(atPosition, callback);
            c.sequencedPosition = c.sequencedEndPosition = atPosition;
            inSequence._sequencedObjs.Add(c);
            if (inSequence.duration < atPosition) inSequence.duration = atPosition;
            return inSequence;
        }

        // ===================================================================================
        // INTERNAL METHODS ------------------------------------------------------------------

        internal override void Reset()
        {
            base.Reset();

            sequencedTweens.Clear();
            _sequencedObjs.Clear();
        }

        // CALLED BY Tween the moment the tween starts.
        // Returns TRUE in case of success (always TRUE for Sequences)
        internal override bool Startup()
        {
            startupDone = true;
            fullDuration = loops > -1 ? duration * loops : Mathf.Infinity;
            // Order sequencedObjs by start position
            _sequencedObjs.Sort(SortSequencedObjs);
            return true;
        }

        // Applies the tween set by DoGoto.
        // Returns TRUE if the tween needs to be killed
        internal override bool ApplyTween(float prevPosition, int prevCompletedLoops, int newCompletedSteps, bool useInversePosition, UpdateMode updateMode)
        {
            float from, to = 0;
            if (updateMode == UpdateMode.Update && newCompletedSteps > 0) {
                // Run all cycles elapsed since last update
                int cycles = newCompletedSteps;
                int cyclesDone = 0;
                from = prevPosition;
                bool isInverse = loopType == LoopType.Yoyo
                    && (prevPosition < duration ? prevCompletedLoops % 2 != 0 : prevCompletedLoops % 2 == 0);
                if (isBackwards) isInverse = !isInverse; // TEST
                while (cyclesDone < cycles) {
                    //                    Debug.Log("::::::::::::: CYCLING : " + stringId + " : " + cyclesDone + " ::::::::::::::::::::::::::::::::::::");
                    if (cyclesDone > 0) from = to;
                    else if (isInverse && !isBackwards) from = duration - from;
                    to = isInverse ? 0 : duration;
                    if (ApplyInternalCycle(from, to, updateMode)) return true;
                    cyclesDone++;
                    if (loopType == LoopType.Yoyo) isInverse = !isInverse;
                }
            }
            // Run current cycle
            //            Debug.Log("::::::::::::: UPDATING");
            if (newCompletedSteps > 0) from = useInversePosition ? duration : 0;
            else from = useInversePosition ? duration - prevPosition : prevPosition;
            return ApplyInternalCycle(from, useInversePosition ? duration - position : position, updateMode);
        }

        // Called by DOTween when spawning/creating a new Sequence.
        internal void Setup()
        {
            isPlaying = DOTween.defaultAutoPlayBehaviour == AutoPlay.All || DOTween.defaultAutoPlayBehaviour == AutoPlay.AutoPlaySequences;
            loopType = DOTween.defaultLoopType;
        }

        // ===================================================================================
        // METHODS ---------------------------------------------------------------------------

        bool ApplyInternalCycle(float fromPos, float toPos, UpdateMode updateMode)
        {
            bool isGoingBackwards = toPos < fromPos;
            if (isGoingBackwards) {
                int len = _sequencedObjs.Count - 1;
                for (int i = len; i > -1; --i) {
                    ABSSequentiable sequentiable = _sequencedObjs[i];
                    if (updateMode == UpdateMode.Update && (sequentiable.sequencedEndPosition < toPos || sequentiable.sequencedPosition > fromPos)) continue;
                    if (sequentiable.tweenType == TweenType.Callback) sequentiable.onStart();
                    else {
                        // Nested Tweener/Sequence
                        float gotoPos = toPos - sequentiable.sequencedPosition;
                        if (gotoPos < 0) gotoPos = 0;
                        Tween t = (Tween)sequentiable;
                        t.isBackwards = true;
//                        Debug.Log("             < " + t.stringId + " " + fromPos + "/" + toPos + " : " + gotoPos);
                        if (TweenManager.Goto(t, gotoPos, false, updateMode)) return true;
                    }
                }
            } else {
                int len = _sequencedObjs.Count;
                for (int i = 0; i < len; ++i) {
                    ABSSequentiable sequentiable = _sequencedObjs[i];
                    if (updateMode == UpdateMode.Update && (sequentiable.sequencedPosition > toPos || sequentiable.sequencedEndPosition < fromPos)) continue;
                    if (sequentiable.tweenType == TweenType.Callback) sequentiable.onStart();
                    else {
                        // Nested Tweener/Sequence
                        float gotoPos = toPos - sequentiable.sequencedPosition;
                        if (gotoPos < 0) gotoPos = 0;
                        Tween t = (Tween)sequentiable;
                        t.isBackwards = false;
//                        Debug.Log("             > " + t.stringId + " " + fromPos + "/" + toPos + " : " + gotoPos);
                        if (TweenManager.Goto(t, gotoPos, false, updateMode)) return true;
                    }
                }
            }
            return false;
        }

        static int SortSequencedObjs(ABSSequentiable a, ABSSequentiable b)
        {
            if (a.sequencedPosition > b.sequencedPosition) return 1;
            if (a.sequencedPosition < b.sequencedPosition) return -1;
            return 0;
        }
    }
}