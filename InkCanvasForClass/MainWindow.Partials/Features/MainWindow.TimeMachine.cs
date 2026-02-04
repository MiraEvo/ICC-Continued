using Ink_Canvas.Helpers;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;

namespace Ink_Canvas {
    public partial class MainWindow {
        private enum CommitReason {
            UserInput,
            CodeInput,
            ShapeDrawing,
            ShapeRecognition,
            ClearingCanvas,
            Manipulation
        }

        private CommitReason _currentCommitType = CommitReason.UserInput;
        private bool IsEraseByPoint => SelectedMode == ICCToolsEnum.EraseByGeometryMode;
        private StrokeCollection ReplacedStroke;
        private StrokeCollection AddedStroke;
        private StrokeCollection CuboidStrokeCollection;
        private Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> StrokeManipulationHistory;

        private Dictionary<Stroke, StylusPointCollection> StrokeInitialHistory = [];

        private Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributesHistory = [];

        private Dictionary<Guid, List<Stroke>> DrawingAttributesHistoryFlag = new() {
            { DrawingAttributeIds.Color, new() },
            { DrawingAttributeIds.DrawingFlags, new() },
            { DrawingAttributeIds.IsHighlighter, new() },
            { DrawingAttributeIds.StylusHeight, new() },
            { DrawingAttributeIds.StylusTip, new() },
            { DrawingAttributeIds.StylusTipTransform, new() },
            { DrawingAttributeIds.StylusWidth, new() }
        };

        private TimeMachine timeMachine = new();

        private void ApplyHistoryToCanvas(TimeMachineHistory item, IccInkCanvasModern applyCanvas = null) {
            _currentCommitType = CommitReason.CodeInput;
            var canvas = inkCanvas;
            if (applyCanvas != null) {
                canvas = applyCanvas;
            }

            if (item.CommitType == TimeMachineHistoryType.UserInput) {
                if (!item.StrokeHasBeenCleared) {
                    foreach (var strokes in item.CurrentStroke)
                        if (!canvas.Strokes.Contains(strokes))
                            canvas.Strokes.Add(strokes);
                } else {
                    foreach (var strokes in item.CurrentStroke)
                        canvas.Strokes.Remove(strokes);
                }
            } else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition) {
                if (item.StrokeHasBeenCleared) {
                    foreach (var strokes in item.CurrentStroke)
                        canvas.Strokes.Remove(strokes);

                    foreach (var strokes in item.ReplacedStroke)
                        if (!canvas.Strokes.Contains(strokes))
                            canvas.Strokes.Add(strokes);
                } else {
                    foreach (var strokes in item.CurrentStroke)
                        if (!canvas.Strokes.Contains(strokes))
                            canvas.Strokes.Add(strokes);

                    foreach (var strokes in item.ReplacedStroke)
                        canvas.Strokes.Remove(strokes);
                }
            } else if (item.CommitType == TimeMachineHistoryType.Manipulation) {
                if (!item.StrokeHasBeenCleared) {
                    foreach (var currentStroke in item.StylusPointDictionary) {
                        if (canvas.Strokes.Contains(currentStroke.Key)) {
                            currentStroke.Key.StylusPoints = currentStroke.Value.Item2;
                        }
                    }
                } else {
                    foreach (var currentStroke in item.StylusPointDictionary) {
                        if (canvas.Strokes.Contains(currentStroke.Key)) {
                            currentStroke.Key.StylusPoints = currentStroke.Value.Item1;
                        }
                    }
                }
            } else if (item.CommitType == TimeMachineHistoryType.DrawingAttributes) {
                if (!item.StrokeHasBeenCleared) {
                    foreach (var currentStroke in item.DrawingAttributes) {
                        if (canvas.Strokes.Contains(currentStroke.Key)) {
                            currentStroke.Key.DrawingAttributes = currentStroke.Value.Item2;
                        }
                    }
                } else {
                    foreach (var currentStroke in item.DrawingAttributes) {
                        if (canvas.Strokes.Contains(currentStroke.Key)) {
                            currentStroke.Key.DrawingAttributes = currentStroke.Value.Item1;
                        }
                    }
                }
            } else if (item.CommitType == TimeMachineHistoryType.Clear) {
                if (!item.StrokeHasBeenCleared) {
                    if (item.CurrentStroke != null)
                        foreach (var currentStroke in item.CurrentStroke)
                            if (!canvas.Strokes.Contains(currentStroke))
                                canvas.Strokes.Add(currentStroke);

                    if (item.ReplacedStroke != null)
                        foreach (var replacedStroke in item.ReplacedStroke)
                            canvas.Strokes.Remove(replacedStroke);
                } else {
                    if (item.ReplacedStroke != null)
                        foreach (var replacedStroke in item.ReplacedStroke)
                            if (!canvas.Strokes.Contains(replacedStroke))
                                canvas.Strokes.Add(replacedStroke);

                    if (item.CurrentStroke != null)
                        foreach (var currentStroke in item.CurrentStroke)
                            canvas.Strokes.Remove(currentStroke);
                }
            }

            _currentCommitType = CommitReason.UserInput;
        }

        private StrokeCollection ApplyHistoriesToNewStrokeCollection(TimeMachineHistory[] items) {
            var fakeInkCanv = new IccInkCanvasModern {
                Width = inkCanvas.ActualWidth,
                Height = inkCanvas.ActualHeight,
                EditingMode = InkCanvasEditingMode.None,
            };

            if (items != null && items.Length > 0) {
                foreach (var timeMachineHistory in items) {
                    ApplyHistoryToCanvas(timeMachineHistory, fakeInkCanv);
                }
            }

            return fakeInkCanv.Strokes;
        }

        private void TimeMachine_OnUndoStateChanged(bool status) {
            SymbolIconUndo.IsEnabled = status;
            // 同步更新 ViewModel 的 CanUndo 状态
            if (ViewModel != null) {
                ViewModel.CanUndo = status;
            }
        }

        private void TimeMachine_OnRedoStateChanged(bool status) {
            SymbolIconRedo.IsEnabled = status;
            // 同步更新 ViewModel 的 CanRedo 状态
            if (ViewModel != null) {
                ViewModel.CanRedo = status;
            }
        }




        private EventHandler _cachedStylusPointsChangedHandler;
        private StylusPointsReplacedEventHandler _cachedStylusPointsReplacedHandler;
        private PropertyDataChangedEventHandler _cachedDrawingAttributesChangedHandler;
        
        private void EnsureEventHandlersCached() {
            _cachedStylusPointsChangedHandler ??= Stroke_StylusPointsChanged;
            _cachedStylusPointsReplacedHandler ??= Stroke_StylusPointsReplaced;
            _cachedDrawingAttributesChangedHandler ??= Stroke_DrawingAttributesChanged;
        }
        
        private void StrokesOnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e) {
            if (!isHidingSubPanelsWhenInking) {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            EnsureEventHandlersCached();

            var removedStrokes = e?.Removed;
            var addedStrokes = e?.Added;
            var removedCount = removedStrokes?.Count ?? 0;
            var addedCount = addedStrokes?.Count ?? 0;

            if (removedCount > 0) {
                foreach (var stroke in removedStrokes) {
                    stroke.StylusPointsChanged -= _cachedStylusPointsChangedHandler;
                    stroke.StylusPointsReplaced -= _cachedStylusPointsReplacedHandler;
                    stroke.DrawingAttributesChanged -= _cachedDrawingAttributesChangedHandler;
                    StrokeInitialHistory.Remove(stroke);
                }
            }

            if (addedCount > 0) {
                foreach (var stroke in addedStrokes) {
                    stroke.StylusPointsChanged += _cachedStylusPointsChangedHandler;
                    stroke.StylusPointsReplaced += _cachedStylusPointsReplacedHandler;
                    stroke.DrawingAttributesChanged += _cachedDrawingAttributesChangedHandler;
                    StrokeInitialHistory[stroke] = stroke.StylusPoints.Clone();
                }
            }

            if (_currentCommitType == CommitReason.CodeInput || _currentCommitType == CommitReason.ShapeDrawing) return;

            if ((addedCount != 0 || removedCount != 0) && IsEraseByPoint) {
                AddedStroke ??= new();
                ReplacedStroke ??= new();
                if (addedCount > 0) AddedStroke.Add(addedStrokes);
                if (removedCount > 0) ReplacedStroke.Add(removedStrokes);
                return;
            }

            if (addedCount != 0) {
                if (_currentCommitType == CommitReason.ShapeRecognition) {
                    timeMachine.CommitStrokeShapeHistory(ReplacedStroke, addedStrokes);
                    ReplacedStroke = null;
                } else {
                    timeMachine.CommitStrokeUserInputHistory(addedStrokes);
                }
                return;
            }

            if (removedCount != 0) {
                if (_currentCommitType == CommitReason.ShapeRecognition) {
                    ReplacedStroke = removedStrokes;
                } else if (!IsEraseByPoint || _currentCommitType == CommitReason.ClearingCanvas) {
                    timeMachine.CommitStrokeEraseHistory(removedStrokes);
                }
            }
        }

        private void Stroke_DrawingAttributesChanged(object sender, PropertyDataChangedEventArgs e) {
            if (sender is not Stroke key) return;
            
            var currentValue = key.DrawingAttributes.Clone();
            DrawingAttributesHistory.TryGetValue(key, out var previousTuple);
            var previousValue = previousTuple?.Item1 ?? currentValue.Clone();
            
            var propertyGuid = e.PropertyGuid;
            var flagList = DrawingAttributesHistoryFlag[propertyGuid];
            var needUpdateValue = !flagList.Contains(key);
            
            if (needUpdateValue) {
                flagList.Add(key);
                #if DEBUG
                Debug.Write(e.PreviousValue.ToString());
                #endif
                
                var prevValue = e.PreviousValue;
                if (propertyGuid == DrawingAttributeIds.Color) {
                    previousValue.Color = (Color)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.IsHighlighter) {
                    previousValue.IsHighlighter = (bool)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.StylusHeight) {
                    previousValue.Height = (double)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.StylusWidth) {
                    previousValue.Width = (double)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.StylusTip) {
                    previousValue.StylusTip = (StylusTip)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.StylusTipTransform) {
                    previousValue.StylusTipTransform = (Matrix)prevValue;
                } else if (propertyGuid == DrawingAttributeIds.DrawingFlags) {
                    previousValue.IgnorePressure = (bool)prevValue;
                }
            }

            DrawingAttributesHistory[key] =
                new Tuple<DrawingAttributes, DrawingAttributes>(previousValue, currentValue);
        }

        private void Stroke_StylusPointsReplaced(object sender, StylusPointsReplacedEventArgs e) {
            if (isMouseGesturing) return;
            if (sender is not Stroke stroke) return;
            StrokeInitialHistory[stroke] = e.NewStylusPoints.Clone();
        }

        private void Stroke_StylusPointsChanged(object sender, EventArgs e) {
            if (isMouseGesturing) return;
            
            if (sender is not Stroke stroke) return;
            
            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            var count = selectedStrokes.Count;
            if (count == 0) count = inkCanvas.Strokes.Count;
            
            StrokeManipulationHistory ??= new(count);

            if (StrokeInitialHistory.TryGetValue(stroke, out var initialPoints)) {
                StrokeManipulationHistory[stroke] =
                    new Tuple<StylusPointCollection, StylusPointCollection>(initialPoints, stroke.StylusPoints.Clone());
            }
            
            if ((StrokeManipulationHistory.Count == count || sender == null) && dec.Count == 0) {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory) {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }
                StrokeManipulationHistory = null;
            }
        }
    }
}
