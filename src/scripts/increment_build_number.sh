#!/bin/bash

# Define the file path
BUILD_INFO_FILE="src/scripts/BuildInfo.cs"

# Check if the file exists
if [ ! -f "$BUILD_INFO_FILE" ]; then
    echo "Error: $BUILD_INFO_FILE not found."
    exit 1
fi

# Use grep and sed to find, increment, and replace the BuildNumber
# 1. grep looks for the line containing BuildNumber
# 2. cut extracts the number (after '=' and spaces)
# 3. awk performs the arithmetic increment
CURRENT_NUMBER=$(grep 'public const int BuildNumber' $BUILD_INFO_FILE | cut -d '=' -f 2 | tr -d ' ;' | awk '{print $1}')

# Check if we successfully got a number (should handle the '0' case)
if [ -z "$CURRENT_NUMBER" ]; then
    echo "Error: Could not extract current BuildNumber."
    exit 1
fi

NEW_NUMBER=$((CURRENT_NUMBER + 1))

# Use sed to replace the entire line with the new, incremented number
# We use a unique separator (|) with sed to avoid issues with slashes in file paths
sed -i "s|public const int BuildNumber = [0-9]\+;|public const int BuildNumber = $NEW_NUMBER;|g" $BUILD_INFO_FILE

echo "Build number incremented from $CURRENT_NUMBER to $NEW_NUMBER."

# Exit successfully
exit 0