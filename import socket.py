import socket
import cv2
import threading
from queue import Queue

HOSTNAME = '127.0.0.1'  # Localhost
PORT = 5010  # Port for listening

# Initialize the camera (attempt to open the default camera)
cap = cv2.VideoCapture(0)
import socket
import cv2
import pickle
import threading
from queue import Queue
from dollarpy import Point
import mediapipe as mp
from ultralytics import YOLO
import time

HOSTNAME = '127.0.0.1'
PORT = 5010

# Initialize the camera (attempt to open the default camera)
cap = cv2.VideoCapture(0)

# Check if the camera opened successfully
if not cap.isOpened():
    print("Error: Could not open video device.")
    exit(1)
else:
    print("Camera successfully opened.")

# Create the socket and start listening
soc = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

try:
    soc.bind((HOSTNAME, PORT))
    soc.listen(5)
    print(f"Server started on {HOSTNAME}:{PORT}. Waiting for a connection...")
except socket.error as e:
    print(f"Error binding socket: {e}")
    exit(1)

conn, addr = soc.accept()  # This will block until a client connects
print(f"Connection established with {addr}")

# Initialize YOLO model
model = YOLO(r"C:\Users\alie0\Downloads\best (2).pt")  # Update path as needed
target_class_names = ["deodorant", "vitamin"]
target_classes = [index for index, name in model.names.items() if name in target_class_names]
print("Target class indices:", target_classes)

# Initialize MediaPipe Hands for gesture recognition
mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands
hands = mp_hands.Hands()

# Lock for synchronizing access to the camera and socket
camera_lock = threading.Lock()
socket_lock = threading.Lock()

# Frame queue for passing frames between threads
frame_queue = Queue(maxsize=10)

# Graceful shutdown flag
shutdown_flag = False

# Function to send data safely
def send_data(msg):
    try:
        with socket_lock:
            if conn.fileno() != -1:  # Check if the socket is still open
                conn.send(msg)
            else:
                print("Socket is closed.")
    except socket.error as e:
        print(f"Socket error: {e}")
        global shutdown_flag
        shutdown_flag = True
        conn.close()

# Camera capture thread function
def capture_frame():
    global shutdown_flag  # Ensure we're modifying the global shutdown_flag
    while not shutdown_flag:
        with camera_lock:
            ret, frame = cap.read()
            if ret:
                if frame_queue.full():
                    frame_queue.get()  # Discard the oldest frame if queue is full
                frame_queue.put(frame)
            else:
                print("Failed to capture frame.")
                shutdown_flag = True  # Stop threads if the camera fails
                break  # Exit the loop if camera fails

# Function for gesture recognition
def capture_gestures():
    global shutdown_flag  # Ensure we're modifying the global shutdown_flag
    with open('gesture_recognizer1.pkl', 'rb') as file:
        recognizer = pickle.load(file)
    
    points = []
    while not shutdown_flag:
        if not frame_queue.empty():
            frame = frame_queue.get()

            # Convert the frame to RGB for MediaPipe processing
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = hands.process(frame_rgb)
            annotated_image = frame.copy()

            if results.multi_hand_landmarks:
                for hand_landmarks in results.multi_hand_landmarks:
                    mp_drawing.draw_landmarks(annotated_image, hand_landmarks, mp_hands.HAND_CONNECTIONS)
                    
                    # Capture important hand landmarks for gesture recognition
                    wrist = Point(hand_landmarks.landmark[0].x, hand_landmarks.landmark[0].y, 1)
                    thumb_cmc = Point(hand_landmarks.landmark[1].x, hand_landmarks.landmark[1].y, 1)
                    thumb_mcp = Point(hand_landmarks.landmark[2].x, hand_landmarks.landmark[2].y, 1)
                    thumb_ip = Point(hand_landmarks.landmark[3].x, hand_landmarks.landmark[3].y, 1)
                    thumb_tip = Point(hand_landmarks.landmark[4].x, hand_landmarks.landmark[4].y, 1)

                    points = [wrist, thumb_cmc, thumb_mcp, thumb_ip, thumb_tip]

                    print(f"Hand landmarks: {points}")  # Debug: Log the landmarks
                    try:
                        # Check if the recognizer is working and making a prediction
                        prediction = recognizer.recognize(points)
                        print(f"Predictions: {prediction}")  # Debugging
                        
                        if prediction and len(prediction) > 0:
                            detected_gesture = prediction[0]
                            print(f"Detected gesture: {detected_gesture}")  # Debugging
                            msg_pred = f"{detected_gesture}".encode("utf-8")
                            if conn.fileno() != -1:  # Check if the connection is still open
                                send_data(msg_pred)  # Send data to the client
                            else:
                                print("Connection closed, unable to send gesture data.")
                    except Exception as e:
                        print("An error occurred:", str(e))
            else:
                print("No hand landmarks detected.")

            # Display the annotated image with landmarks
            cv2.imshow('Camera - Gesture Recognition', annotated_image)

            # Exit if the 'q' key is pressed
            if cv2.waitKey(1) & 0xFF == ord('q'):
                shutdown_flag = True
                break

# Function for YOLO-based object detection
def detect_objects():
    global shutdown_flag  # Ensure we're modifying the global shutdown_flag
    detected_objects = []

    while not shutdown_flag:
        if not frame_queue.empty():
            frame = frame_queue.get()

            # Resize the frame to 640x640 (YOLO default size)
            frame_resized = cv2.resize(frame, (640, 640))

            # Perform YOLO inference with a higher confidence threshold (e.g., 0.9)
            results = model(frame_resized, conf=0.9, classes=target_classes)  # Increased confidence threshold

            # Get the first result
            result = results[0]

            # Annotate the frame with the results
            annotated_frame = result.plot()  # Adds bounding boxes, labels, etc.

            # Check if there are detections
            if len(result.boxes.cls) > 0:
                print("Detected classes and their confidences:")
                for box, score, cls in zip(result.boxes.xywh, result.boxes.conf, result.boxes.cls):
                    class_name = model.names[int(cls)]  # Get class name
                    confidence = float(score)  # Get confidence score
                    print(f"Class: {class_name}, Confidence: {confidence:.2f}")

                    # Only add target classes to the list
                    if class_name in target_class_names:
                        detected_objects.append((class_name, confidence))
                        print(f"Detected target class: {class_name} with confidence: {confidence:.2f}")
                        
                        # Send detected object class name to the client
                        msg_class = f"Detected Object: {class_name}".encode("utf-8")
                        send_data(msg_class)  # Send detected class name to the client
            else:
                print("No target objects detected.")

            # Display the annotated frame
            cv2.imshow("Camera - Object Detection", annotated_frame)

            # Wait for the 'q' key to be pressed, and break the loop
            if cv2.waitKey(1) & 0xFF == ord('q'):
                print("\n--- Detected Objects ---")
                for obj in detected_objects:
                    print(f"Class: {obj[0]}, Confidence: {obj[1]:.2f}")
                shutdown_flag = True
                break

# Start the threads for gesture recognition and object detection
camera_thread = threading.Thread(target=capture_frame)
gesture_thread = threading.Thread(target=capture_gestures)
object_detection_thread = threading.Thread(target=detect_objects)

# Start all threads
camera_thread.start()
gesture_thread.start()
object_detection_thread.start()

# Wait for the threads to finish
camera_thread.join()
gesture_thread.join()
object_detection_thread.join()

cap.release()
cv2.destroyAllWindows()
conn.close()
soc.close()
# Check if the camera opened successfully
if not cap.isOpened():
    print("Error: Could not open video device.")
    exit(1)

# Create the socket and start listening
soc = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
soc.bind((HOSTNAME, PORT))
soc.listen(5)
print(f"Server started. Waiting for a connection on {HOSTNAME}:{PORT}...")
conn, addr = soc.accept()  # This will block until a client connects
print("Device connected")

# Function to send data safely
def send_data(msg):
    try:
        if conn.fileno() != -1:  # Check if the socket is open
            conn.send(msg)
    except Exception as e:
        print(f"Socket error: {e}")

# Camera capture thread function
def capture_frame():
    while True:
        ret, frame = cap.read()
        if not ret:
            print("Failed to capture frame from the camera.")
            break
        
        # Display the frame in a window
        cv2.imshow("Camera", frame)  # Display the frame in the window "Camera"
        
        # Process frame (this could be enhanced with other operations like sending to the client)
        print("Frame captured successfully.")

        # Wait for the key event and exit when 'q' is pressed
        if cv2.waitKey(1) & 0xFF == ord('q'):
            print("Exiting...")
            break

# Start the capture frame thread
camera_thread = threading.Thread(target=capture_frame)
camera_thread.start()

# Wait for the threads to finish
camera_thread.join()

# Release resources and close windows
cap.release()
cv2.destroyAllWindows()
conn.close()
soc.close()