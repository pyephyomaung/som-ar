<h1>Augmented Reality using a Neural Network</h1>

<div><strong>Platform:</strong> .Net (C#), OpenCV</div>

<div>
  <strong>Abstract:</strong> <p>We present a simple augmented reality system using a neural network inspired
by the biological vision system. It uses a feature-based recognition system developed in
the C# programming environment. It uses OpenCV for feature detection and extraction, a
self-organizing map (SOM) for picture recognition, and Microsoft WPF 3D for rendering
3D objects on the screen. This paper describes the characteristics of augmented reality
system. For image recognition, the global representation of images are used as image fea-
tures. We explore different global features that can be used to improve the recognition and
discuss how the self-organizing map (SOM) is trained using these global features using an
unsupervised learning algorithm. The SOM is a two-layer neural network comprising the
input layer and the competition layer that is a grid of neurons. Each neuron is represented
by a vector that has the same dimension as an input feature vector. During the training, the
SOM groups the similar pictures together in the competition layer and chooses a neuron
for each group.</p>
<p>We describe the process of building an augmented reality system using the self-organizing
map. Using a webcam as a visual sensor, the system is presented with a sequence of in-
put images that are processed frame by frame. On receiving an input image, the system
detects a rectangle of interest in the input image by analyzing contours from the edges ex-
tracted with an edge detection algorithm in OpenCV. From the picture within the rectangle
of interest, a feature vector is extracted and later fed into the neural network for picture
recognition. Picture recognition is done by choosing the best representative neuron that
represents a group of pictures in the competition layer, and then choosing the most similar
picture in the group. Once a picture is recognized, a 3D object corresponding to the picture
is rendered on the screen using WPF 3D.</p>
</div>

Paper, Source code and prezi slides can be found here:<br/>
http://pyephyo.wordpress.com/2012/04/04/augmented-reality-using-a-neural-network/
