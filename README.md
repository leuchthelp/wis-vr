## Setup

### Getting Started:

-----
#### 1. First: Install a code editor (Visual Studio Code)
For ease of use managing the files, we are going to use [Visual Studio Code](https://code.visualstudio.com/download). 

Head on over to the `Downloads` page to download and install `VSC`.

Once installed open the editor (`VSC`) and select the `Source Control` tab on the left hand side. It should be the third entry from the top.

----
#### 2. Second: Generate a Gitlab Access Token

##### 2.1 Install OVGU VPN:

In order to fully utilize https://code.ovgu.de/flheinri/tai25-team2 you will need to install the [OVGU VPN](https://www.urz.ovgu.de/eduvpn.html). This is required to be able to use `git` to `push` and `pull` code & changes from and to the repository we are going to setup later https://www.urz.ovgu.de/en/-p-4448.html.

##### 2.2 Access OVGU Gitlab:
Now we want to head on over to https://code.ovgu.de/flheinri/tai25-team2. Log into the OVGU-Gitlab instance with your university sign-on.

Afterwards click on your `Profile-Icon` in the top left corner and click on `Preferences`. From hear select `Personal Access Token` in the left drop-down menu and click on `Add new token` in the top right corner.

Give the new `Token` a `Token Name` to remember the token by. The `Expiration Date` can stay the same.

For the `Scope` select `api` and click on `Create Token`. 

Now your token should be at the top of the page under `Personal Access Tokens`. You can either show the token or `copy` it. 

IMPORTANT: Copy the token for later use, you will not be shown the same token again. Once you leave the page or refresh it, the token will be gone and you will have to create a new one.

-----
#### 3. Third: Add the Repository to your  Source Control
Back in `VSC` you want to click on `Clone Repository` and add the project link. You need to replace the `ACCESS-TOKEN` portion with the `Gitlab Access Token` we just created.

https://oauth2:ACCESS-TOKEN@code.ovgu.de/flheinri/tai25-team2.git

Select the `Destination directory` to save the repository to and press on `Open`. 

You should now be able to see all related project files. 

-----
#### 4. Fourth: Open the Unity Project in Unity Hub
Download and install [Unity Hub](https://unity.com/download). Once finished, open Unity Hub and head on over the `Projects` tab. Click on `Add` in the top right and then select `Add project from disk`. Find the location your save the repository to from the prior step and hit open. 

The Project should now have been loaded and the required version of Unity should be shown under the `Editor Version`.

Double click on the project to launch it and wait for Unity to finish. Unity should now install all required packages and files needed by the project.

Once the project has finished loading and the `Unity Editor` has started up, select the `Impairment` folder in the `Project` window, located on the bottom left and open the `Impairment Project` scene. 

In here you should find one `Main Camera`, three `Spheres` under the `Sphere Parent` and one `Global Volume`. Click on the `Global Volume` and play around with the `Intensity` slider and `Mode` dropdown in the right `Inspector` tab to cycle through the different `Impairment Modes`.