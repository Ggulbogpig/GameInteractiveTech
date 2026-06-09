# from ultralytics import YOLO
# import socket
# import json
# import cv2

# # YOLO
# model = YOLO("yolov8n.pt")

# # TCP Server
# server = socket.socket(socket.AF_INET,
#                        socket.SOCK_STREAM)

# server.bind(("0.0.0.0",9999))
# server.listen(1)

# print("Waiting Unity...")

# conn, addr = server.accept()

# print("Connected:", addr)

# cap = cv2.VideoCapture(0)

# while True:

#     ret, frame = cap.read()

#     if not ret:
#         continue

#     results = model(frame)

#     detections = []

#     for result in results:
#         for box in result.boxes:

#             cls = int(box.cls[0])

#             name = model.names[cls]

#             if name == "person":

#                 x1,y1,x2,y2 = box.xyxy[0]

#                 cx = float((x1+x2)/2)
#                 cy = float((y1+y2)/2)

#                 msg = json.dumps(
#                 {
#                     "className":"person",
#                     "cx":cx,
#                     "cy":cy
#                 })

#                 conn.send(
#                     (msg+"\n").encode())

#                 break



##임시 테스트용
from ultralytics import YOLO
import socket
import json
import cv2
import time

model = YOLO("yolov8n.pt")

server = socket.socket(
    socket.AF_INET,
    socket.SOCK_STREAM)

server.bind(("0.0.0.0",9999))
server.listen(1)

print("Waiting Unity...")

conn, addr = server.accept()

print("Connected:", addr)

cap = cv2.VideoCapture(0)

prev_x = None
prev_z = None
prev_time = time.time()

while True:

    ret, frame = cap.read()

    if not ret:
        continue

    results = model(frame)

    for result in results:

        for box in result.boxes:

            cls = int(box.cls[0])
            name = model.names[cls]

            if name != "person":
                continue

            x1,y1,x2,y2 = box.xyxy[0]

            cx = float((x1+x2)/2)
            cy = float((y1+y2)/2)

            # -------------------
            # 임시 World Position
            # -------------------

            world_x = (cx - 320.0) / 100.0
            world_z = 2.0

            now = time.time()
            dt = max(now - prev_time, 0.001)

            if prev_x is None:
                vx = 0.0
                vz = 0.0
            else:
                vx = (world_x - prev_x) / dt
                vz = (world_z - prev_z) / dt

            prev_x = world_x
            prev_z = world_z
            prev_time = now

            msg = json.dumps(
            {
                "className":"person",

                "x":world_x,
                "z":world_z,

                "vx":vx,
                "vz":vz
            })

            conn.send(
                (msg + "\n").encode())

            break