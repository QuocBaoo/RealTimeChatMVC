#!/bin/bash
# Test script để verify API Upload File

# Cấu hình
API_URL="http://localhost:5000"
TEST_FILE="test.txt"

# 1. Tạo file test
echo "📁 Tạo file test..."
echo "Hello, this is a test file for upload API" > "$TEST_FILE"

# 2. Test upload file
echo "📤 Testing /Files/Upload endpoint..."
curl -X POST \
  -F "file=@$TEST_FILE" \
  "$API_URL/Files/Upload"

echo ""
echo "✅ Test hoàn thành"
echo "💡 Kiểm tra thư mục: wwwroot/uploads/"

# 3. Dọn dẹp
rm "$TEST_FILE"
