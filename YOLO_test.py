from ultralytics import YOLO
import socket
import json
import cv2

# YOLO
model = YOLO("yolov8n.pt")

# TCP Server
server = socket.socket(socket.AF_INET,
                       socket.SOCK_STREAM)

server.bind(("0.0.0.0",9999))
server.listen(1)

print("Waiting Unity...")

conn, addr = server.accept()

print("Connected:", addr)

cap = cv2.VideoCapture(0)

while True:

    ret, frame = cap.read()

    if not ret:
        continue

    results = model(frame)

    detections = []

    for result in results:
        for box in result.boxes:

            cls = int(box.cls[0])

            name = model.names[cls]

            if name == "person":

                x1,y1,x2,y2 = box.xyxy[0]

                cx = float((x1+x2)/2)
                cy = float((y1+y2)/2)

                msg = json.dumps(
                {
                    "className":"person",
                    "cx":cx,
                    "cy":cy
                })

                conn.send(
                    (msg+"\n").encode())

                break