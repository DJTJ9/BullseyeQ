#!/bin/bash
# Run Unity EditMode tests in batch mode
UNITY="/c/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe"
PROJECT="C:/Unity/Aktuelle Projekte/DartTrainingsApp"
RESULTS="$PROJECT/test-results.xml"
LOG="$PROJECT/unity-test.log"

echo "Running EditMode tests..."
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJECT" \
  -runTests -testPlatform EditMode \
  -testResults "$RESULTS" \
  -logFile "$LOG"

EXIT=$?
echo "Log:     $LOG"
echo "Results: $RESULTS"
exit $EXIT
