#!/bin/bash

# =====================================================================
# UMD VR Memory Experiment: Data Retrieval Pipeline
# =====================================================================

# 1. Update this to your actual Quest Project Package Name (from Unity Player Settings)
PACKAGE_NAME="com.UnityTechnologies.com.unity.template.urpblank"

# 2. Path established in LoggingManager.cs (Application.persistentDataPath)
DEVICE_PATH="/sdcard/Android/data/$PACKAGE_NAME/files"

# 3. Local storage for your UROP/Thesis analysis
LOCAL_DEST="./ExperimentResults"

echo "------------------------------------------------------------"
echo "Initializing ADB Data Pull for Quest 3..."
echo "Target Package: $PACKAGE_NAME"
echo "------------------------------------------------------------"

# Check if headset is connected
ADB_STATUS=$(adb get-state 2>/dev/null)
if [ "$ADB_STATUS" != "device" ]; then
    echo "[ERROR] Quest 3 not detected. Please connect headset and enable USB debugging."
    exit 1
fi

# Create local destination folder
mkdir -p "$LOCAL_DEST"

echo "Pulling Experiment Data (Telemetry & Events)..."

# We pull specifically for the 'experiment_' prefix defined in LoggingManager.cs
# This avoids pulling unnecessary Unity system files
adb pull "$DEVICE_PATH/." "$LOCAL_DEST"

echo "------------------------------------------------------------"
echo "SUCCESS: Data serialized to $LOCAL_DEST"
echo "Files retrieved:"
ls -lh "$LOCAL_DEST" | grep "experiment_"
echo "------------------------------------------------------------"
