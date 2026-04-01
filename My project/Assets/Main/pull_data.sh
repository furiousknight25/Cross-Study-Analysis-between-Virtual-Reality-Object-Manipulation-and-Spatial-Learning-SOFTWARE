#!/bin/bash

# --- Configuration ---
PACKAGE_NAME="com.UnityTechnologies.com.unity.template.urpblank"
# The path inside the Quest 3 where Unity's Application.persistentDataPath points
DEVICE_PATH="/sdcard/Android/data/$PACKAGE_NAME/files/"
# The folder on your PC where you want the data to land
LOCAL_DEST="./ExperimentResults"

echo "Looking for Quest 3 headset..."
adb devices

echo "Creating local destination folder: $LOCAL_DEST"
mkdir -p "$LOCAL_DEST"

echo "Pulling data files from headset..."
# We pull the entire 'files' directory to ensure we grab all timestamped CSVs at once
adb pull "$DEVICE_PATH." "$LOCAL_DEST"

echo "Data successfully pulled!"
