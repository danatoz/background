!bin/bash

for i in {1..5}; do
    curl -v -X POST http://localhost:5293/messages \
         -H "Content-Type: application/json" \
         -d '{"payload": "hello"}'
done

