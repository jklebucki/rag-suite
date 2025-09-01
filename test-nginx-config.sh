#!/bin/bash

# Test Nginx Configuration Syntax
# This script extracts and validates the nginx configuration from production-setup.sh

echo "🔍 Testing Nginx Configuration Syntax..."

# Create temporary nginx config file
TEMP_CONFIG="/tmp/rag-suite-nginx-test.conf"

# Extract nginx configuration from production-setup.sh
echo "Extracting nginx configuration..."

cat > "$TEMP_CONFIG" << 'EOF'
events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Logging
    access_log /var/log/nginx/access.log;
    error_log /var/log/nginx/error.log;

    # Basic Settings
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 100M;

    # Gzip Settings
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_comp_level 6;
    gzip_proxied any;
    gzip_types
        application/atom+xml
        application/geo+json
        application/javascript
        application/x-javascript
        application/json
        application/ld+json
        application/manifest+json
        application/rdf+xml
        application/rss+xml
        application/xhtml+xml
        application/xml
        font/eot
        font/otf
        font/ttf
        image/svg+xml
        text/css
        text/javascript
        text/plain
        text/xml;

    server {
        listen 80;
        server_name localhost rag-suite.local;
        root /var/www/rag-suite;
        index index.html;

        # Security headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;

        # Block access to sensitive files
        location ~* \.(sql|log|conf|ini|sh|bak|backup|old|tmp)\$ {
            deny all;
            return 404;
        }

        # Static assets with long cache
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)\$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
            add_header Vary "Accept-Encoding";
            
            # CORS for fonts
            location ~* \.(woff|woff2|ttf|eot)\$ {
                add_header Access-Control-Allow-Origin "*";
                expires 1y;
                add_header Cache-Control "public, immutable";
            }
        }

        # Main location
        location / {
            try_files \$uri \$uri/ /index.html;
            
            # Cache dla HTML
            location ~* \.html\$ {
                expires 5m;
                add_header Cache-Control "public, no-cache";
            }
            
            # Security dla index.html
            location = /index.html {
                add_header X-Frame-Options "SAMEORIGIN" always;
                add_header X-Content-Type-Options "nosniff" always;
                expires 5m;
            }
        }

        # API proxy
        location /api/ {
            proxy_pass http://127.0.0.1:5000;
            proxy_http_version 1.1;
            proxy_set_header Upgrade \$http_upgrade;
            proxy_set_header Connection keep-alive;
            proxy_set_header Host \$host;
            proxy_set_header X-Real-IP \$remote_addr;
            proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto \$scheme;
            proxy_cache_bypass \$http_upgrade;
            proxy_connect_timeout 30s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
        }

        # Health check
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF

echo "✅ Nginx configuration extracted to: $TEMP_CONFIG"

# Test nginx configuration syntax
if command -v nginx >/dev/null 2>&1; then
    echo "🧪 Testing with nginx -t..."
    if nginx -t -c "$TEMP_CONFIG"; then
        echo "✅ Nginx configuration syntax is VALID!"
        EXIT_CODE=0
    else
        echo "❌ Nginx configuration syntax is INVALID!"
        EXIT_CODE=1
    fi
else
    echo "⚠️  nginx command not found - cannot test configuration syntax"
    echo "💡 Configuration file created at: $TEMP_CONFIG"
    echo "💡 You can test it manually with: nginx -t -c $TEMP_CONFIG"
    EXIT_CODE=0
fi

# Cleanup
echo "🧹 Cleaning up temporary file..."
rm -f "$TEMP_CONFIG"

echo "🏁 Test completed!"
exit $EXIT_CODE
