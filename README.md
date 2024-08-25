# Snakes and Ladders

## Serious Game
The game developed in this project is for Android, built with Unity2D. 2 main features of this snakes and ladders game: 1) The board game itself, 2) Popup screen to capture image and send it to model. This game was built as a serious game for autistic children to learn facial expressions. User plays the game, and when they encounter a snake or ladder, the game then prompts them to mimic a certain expression then calculates their expression's accuracy.
This game communicates with a model on a server, sending images as requests and receiving prediction results to be displayed to user and calculates as game score. More details about the flow can be found in the paper and in the video.

## Model
The model used in this project is `mobilenet.keras` in folder `FER-API`. This project compared 2 different pre-trained models as its base model: VGG16 and MobileNet. Both models are trained on a modified FER2013 dataset in `Link Dataset.txt`. Both notebooks are in the folder `FER-API`, also accessible online at:
1. https://www.kaggle.com/code/sarahhpth/mobilenet-kfold-fer-augmented (MobileNet) 
2. https://www.kaggle.com/code/sarahhpth/vgg16-kfold-fer-augmented (VGG16)

All modelling process was done in Kaggle using its environment. If you choose not to train your model locally, I recommend using Kaggle over Colab since it has more free GPU resources and you can track your limit. Kaggle also uses the latest TensorFlow and Keras version. You can use any notebook, remote or local, but you might need to adjust the environment here and there. Anyway, if you choose so, you might want to install these:
1. `tensorflow==2.16.1`
2. Keras `3.2.1`
3. Other libraries in the notebook

If you're not training on Kaggle, use the dataset's API command to import it to your notebook:
```
# make sure kaggle.json is in the location ~/.kaggle/kaggle.json
!mkdir -p ~/.kaggle
!cp kaggle.json ~/.kaggle/
!chmod 600 ~/.kaggle/kaggle.json

! kaggle datasets download -d prilia/fer2013pluscleanedaugmballanced1
!unzip -q fer2013pluscleanedaugmballanced1.zip
```

Once you're done training, save the model in `.keras` format (latest from KerasV3). It's highly recommended since the whole project and the Docker image itself use TensorFlow 2.16 which automatically use KerasV3. Place your model in the same directory as `main.py`.

## API
`FER-API` is deployed in GCP using Cloud Run. Feel free to modify the image processing method in `main.py` if needed. All required libraries for the container are in `requirements.txt`. Build the Docker image, then push it to Cloud Run. Here's how to do it, make sure you're in the same directory as your Dockerfile:

**1. Build your Docker image with Google Container Registry domain**

IMAGE_URL = `gcr.io/<your-project-id>/<image-name>:<tag>`

`<your-project-id>` = Your GCP project ID

`<image-name>` = Your desired image name

`<tag>` = Optional. Image tag can be found in your Docker Desktop. Usually it's `latest`

```
docker build . --tag <IMAGE_URL>
``` 
or:
```
docker build -t <IMAGE_URL> .
```

If you're using Apple M1 or later use this command to avoid deployment error: 
```
docker build . --tag <IMAGE_URL> --platform linux/amd64
```
**2. Push the image**
```
docker push <IMAGE_URL>
```
**3. Deploy to Cloud Run**
```
gcloud run deploy fer-app --image <IMAGE_URL> --platform managed --memory 4Gi
```
Adjust the memory size if needed.