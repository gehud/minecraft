using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Minecraft {
    public readonly struct NativeCurve : IDisposable {
        public readonly NativeArray<Keyframe> Keyframes;

        private const float defaultWeight = 0.0f;
        private const int maxLookaheadCount = 3;

        public NativeCurve(AnimationCurve curve, Allocator allocator) {
            Keyframes = new NativeArray<Keyframe>(curve.keys, allocator);
        }

        public float Evaluate(float time) {
            var startIndex = 0;
            var endIndex = Keyframes.Length - 1;

            if (endIndex <= startIndex) {
                return Keyframes[startIndex].value;
            }

            time = math.clamp(time, Keyframes[startIndex].time, Keyframes[endIndex].time);
            FindIndexForSampling(time, startIndex, endIndex, -1, out int leftIndex, out int rightIndex);

            Keyframe left = Keyframes[leftIndex];
            Keyframe right = Keyframes[rightIndex];
            return InterpolateKeyframe(left, right, time);
        }

        private void FindIndexForSampling(float time, int start, int end, int hint, out int left, out int right) {
            if (hint != -1) {
                hint = math.clamp(hint, start, end);

                if (time > Keyframes[hint].time) {
                    for (int i = 0; i < maxLookaheadCount; i++) {
                        int index = hint + i;
                        if (index + 1 < end && Keyframes[index + 1].time > time) {
                            left = index;
                            right = math.min(left + 1, end);
                            return;
                        }
                    }
                }
            }

            int length = end - start;
            int half;
            int middle;
            int first = start;
            while (length > 0) {
                half = length >> 1;
                middle = first + half;

                var mid = Keyframes[middle];
                if (time < mid.time) {
                    length = half;
                } else {
                    first = middle;
                    ++first;
                    length = length - half - 1;
                }
            }

            left = first - 1;
            right = math.min(end, first);
        }

        private float FastCbrtPositive(float x) {
            return math.exp(math.log(x) / 3.0f);
        }

        private float FastCbrt(float x) {
            return x < 0.0f ? -math.exp(math.log(-x) / 3.0f) : math.exp(math.log(x) / 3.0f);
        }

        private float BezierExtractU(float time, float w1, float w2) {
            var a = 3.0f * w1 - 3.0f * w2 + 1.0f;
            var b = -6.0f * w1 + 3.0f * w2;
            var c = 3.0f * w1;
            var d = -time;

            if (math.abs(a) > 1e-3f) {
                var p = -b / (3.0f * a);
                var p2 = p * p;
                var p3 = p2 * p;

                var q = p3 + (b * c - 3.0f * a * d) / (6.0f * a * a);
                var q2 = q * q;

                var r = c / (3.0f * a);
                var rmp2 = r - p2;

                var s = q2 + rmp2 * rmp2 * rmp2;

                if (s < 0.0f) {
                    var ssi = math.sqrt(-s);
                    var r1 = math.sqrt(-s + q2);
                    var phi = math.atan2(ssi, q);

                    var r3 = FastCbrtPositive(r1);
                    var phi3 = phi / 3.0f;

                    var u1 = 2.0f * r3 * math.cos(phi3) + p;
                    var u2 = 2.0f * r3 * math.cos(phi3 + 2.0f * math.PI / 3.0f) + p;
                    var u3 = 2.0f * r3 * math.cos(phi3 - 2.0f * math.PI / 3.0f) + p;

                    if (u1 >= 0.0f && u1 <= 1.0f) {
                        return u1;
                    } else if (u2 >= 0.0f && u2 <= 1.0f) {
                        return u2;
                    } else if (u3 >= 0.0f && u3 <= 1.0f) {
                        return u3;
                    }

                    return time < 0.5f ? 0.0f : 1.0f;
                } else {
                    float ss = math.sqrt(s);
                    float u = FastCbrt(q + ss) + FastCbrt(q - ss) + p;

                    if (u >= 0.0f && u <= 1.0f) {
                        return u;
                    }

                    return time < 0.5f ? 0.0f : 1.0f;
                }
            }

            if (math.abs(b) > 1e-3f) {
                float s = c * c - 4.0f * b * d;
                float ss = math.sqrt(s);

                float u1 = (-c - ss) / (2.0f * b);
                float u2 = (-c + ss) / (2.0f * b);

                if (u1 >= 0.0f && u1 <= 1.0f) {
                    return u1;
                } else if (u2 >= 0.0f && u2 <= 1.0f) {
                    return u2;
                }

                return time < 0.5f ? 0.0f : 1.0f;
            }

            if (math.abs(c) > 1e-3f) {
                return -d / c;
            }

            return 0.0f;
        }

        private float BezierInterpolate(float time, float v1, float m1, float w1, float v2, float m2, float w2) {
            float u = BezierExtractU(time, w1, 1.0f - w2);
            return BezierInterpolate(u, v1, w1 * m1 + v1, v2 - w2 * m2, v2);
        }

        private float BezierInterpolate(float time, float p0, float p1, float p2, float p3) {
            float t2 = time * time;
            float t3 = t2 * time;
            float omt = 1.0f - time;
            float omt2 = omt * omt;
            float omt3 = omt2 * omt;

            return omt3 * p0 + 3.0f * time * omt2 * p1 + 3.0f * t2 * omt * p2 + t3 * p3;
        }

        private float BezierInterpolate(float time, Keyframe left, Keyframe right) {
            float leftOutWeight = (left.weightedMode & WeightedMode.Out) != 0 ? left.outWeight : defaultWeight;
            float rightInWeight = (right.weightedMode & WeightedMode.In) != 0 ? right.inWeight : defaultWeight;

            float dx = right.time - left.time;
            if (dx == 0.0f) {
                return left.value;
            }

            return BezierInterpolate((time - left.time) / dx, left.value, left.outTangent * dx, leftOutWeight,
                right.value, right.inTangent * dx, rightInWeight);
        }

        private float HermiteInterpolate(float time, Keyframe left, Keyframe right) {
            float dx = right.time - left.time;
            float m1;
            float m2;
            float t;
            if (dx != 0.0f) {
                t = (time - left.time) / dx;
                m1 = left.outTangent * dx;
                m2 = right.inTangent * dx;
            } else {
                t = 0.0f;
                m1 = 0;
                m2 = 0;
            }

            return HermiteInterpolate(t, left.value, m1, m2, right.value);
        }

        private float HermiteInterpolate(float t, float p0, float m0, float m1, float p1) {
            float t2 = t * t;
            float t3 = t2 * t;

            float a = 2.0f * t3 - 3.0f * t2 + 1.0f;
            float b = t3 - 2.0f * t2 + t;
            float c = t3 - t2;
            float d = -2.0f * t3 + 3.0f * t2;

            return a * p0 + b * m0 + c * m1 + d * p1;
        }

        private void HandleSteppedCurve(Keyframe left, Keyframe right, ref float value) {
            if (float.IsInfinity(left.outTangent) || float.IsInfinity(right.inTangent)) {
                value = left.value;
            }
        }

        private float InterpolateKeyframe(Keyframe left, Keyframe right, float time) {
            float output;

            if ((left.weightedMode & WeightedMode.Out) != 0 || (right.weightedMode & WeightedMode.In) != 0) {
                output = BezierInterpolate(time, left, right);
            } else {
                output = HermiteInterpolate(time, left, right);
            }

            HandleSteppedCurve(left, right, ref output);

            return output;
        }

        public void Dispose() {
            Keyframes.Dispose();
        }
    }
}