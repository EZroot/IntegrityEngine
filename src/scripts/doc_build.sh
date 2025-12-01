#!/bin/bash

# --- Configuration ---
# Set the path to your main C# project file (.csproj)
PROJECT_PATH="./Integrity.csproj"

# Directory where the final static HTML site will be generated
DOC_OUTPUT_DIR="_site"

# Name of the generated configuration file (must exist now!)
CONFIG_FILE="docfx.json"

# --- Script Start ---

echo "Starting Documentation Generation for Integrity Engine (v2.0)..."

# 1. Ensure DocFX is available as a .NET tool
echo "Checking for DocFX tool..."
if ! dotnet tool list -g | grep -q "docfx"; then
    echo "DocFX not found. Installing globally..."
    dotnet tool install -g docfx
else
    echo "DocFX found."
fi

# 2. Build the C# project to generate the XML documentation file
# The /p:SkipDocBuild=true flag correctly prevents recursion.
echo "Building project to generate XML documentation..."
dotnet build "$PROJECT_PATH" -c Release /p:SkipDocBuild=true

if [ $? -ne 0 ]; then
    echo "❌ ERROR: dotnet build failed. Check your project path and dependencies."
    exit 1
fi

# 3. Check for the essential configuration file (NO INITIALIZATION ATTEMPT)
if [ ! -f "$CONFIG_FILE" ]; then
    echo "❌ ERROR: The required DocFX configuration file ($CONFIG_FILE) was not found."
    echo "Please ensure the stable docfx.json file is present in the project root."
    exit 1
fi

echo "Using existing DocFX configuration file ($CONFIG_FILE)."

# 4. Build the Documentation site
echo "Building the static documentation site..."
docfx build

# 5. Check for success and provide next steps
if [ $? -eq 0 ]; then
    echo "=================================================="
    echo "✅ Documentation successfully generated!"
    echo "Site is located in: $DOC_OUTPUT_DIR"
    echo "To view, open the index.html file in your browser."
    echo "Example: firefox $DOC_OUTPUT_DIR/index.html"
    echo "=================================================="
else
    echo "❌ ERROR: DocFX build failed. Check the console output and $CONFIG_FILE."
    exit -1
fi

# --- Script End ---