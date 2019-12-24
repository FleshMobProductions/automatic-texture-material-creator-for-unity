# Texture Auto Material Creator

Editor Tool to auto create a set of materials for all textures inside a specific folder in an Unity project. Useful if a model from a 3d application was imported, but materials from it were not properly imported/created or are not Unity compatible. 

## How to Use

Drop the contents of this repository somewhere inside the "Assets" folder of your Unity project. After the Unity Editor is done recompiling the scripts, you should see the menu "FMPUtils" at the top. Click on it and select "Texture Material Creator". The tool takes a folder as input from which all textures will be used to create separate materials with the individual textures set as main texture on the material. After the texture input folder and the material output folder were chosen, clicking on "Create Materials" will generate the new material assets. The tool will take all textures that are either inside the input folder or any subfolder of this folder. Input and output folders have to be inside the project's "Assets" folder. 


## Editor Window Properties


### **Material Prefix**

Appends a string at the start of the material file name. 

### **Material Suffix**

Appends a string at the end of the material file name. 

Generated materials will receive the same name as the textures they are associated with, except if one the properties **Material Prefix** and/or **Material Suffix** are edited. The result name follows the pattern 
```cs
string fileName = materialPrefix + textureName + materialSuffix;
```
Input strings are sanetized to be Windows valid file names. Not tested on Linux. 

### **Template Material**

If set, the new materials will have the same properties and settings as this material. If not set, the base material is the blank material with the "Standard" shader assigned. 

### **Shader Override**

If set, the new material will receive this property as shader and it will override the shader set from the template material. 

### **Overwrite existing materials?**

If true, will replace existing materials in the output folder that have the same name as any of the generated materials. if false, will not replace existing materials. 

### Albedo texture "Alpha is Transparency" Settings

Section for setting a new "Alpha is Transparency" value to overwrite the current property of the Texture Import settings, if necessary

### **Override alphaIsTransparency**

If checked, will set the "Alpha is Transparency" property in the texture import settings of the input texture according to the value of **alphaIsTransparency new value**. 

### **alphaIsTransparency new value**

See text above ("Override alphaIsTransparency"). 

### **Material Albedo Map Settings**

Section about the main/albedo map properties for the new material

### **Use custom material albedo property**

By default, the input texture is assigned to the "_MainTex" property of the shader (the default property used by Unity when Material.mainTexture is set). If checked, will assign the texture to the **Shader Texture Property** value instead. 

### **Shader Texture Property**

See text above ("Use custom material property). 

### **Material Normal Map Settings**

Section about normal map properties for the new material, if normal map assignment should be activated in the material creation process

### **Include normal maps**

If checked, will try to find normal maps for the albedo maps and will apply them to the new materials. The names of the nromal maps need to start with the same file name as the albedo maps and need to end with "_n" or "_normal". For the albedo maps, texture names ending with "_n" or "_normal" are skipped for this purpose. Example: a normal map that has the name and the file ending "Albedo_1.png" needs to have another map in the input folder called either "Albedo_1_n.png" or "Albedo_1_normal.png" or can even have some random letters between the albedo name and the normal map suffix identifier like "Albedo_1_BumpMap_n.png". 

### **Use custom material normal map property**

Built in Unity shaders use the "_BumpMap" property for normal maps/textures. If that is not the desired property name, check this setting to enable setting a custom normal map property name instead. 

### **Normal Map Property**

If **Use custom normal map property** is checked, will use the value of the text field as new property name for assigning the normal texture instead of "_BumpMap". 

### **Folder selection**

Shows the path of the selected folders for the texture input and the material output path. Note: The tool will try to process all textures that are either **inside the input folder or inside any subfolders within the input folder**. 


## Buttons

### **Use folder of selected asset as input**

If an asset in the project view is selected, will assign it to the input folder path used for the textures to process

### **Use folder of selected asset as output**

If an asset in the project view is selected, will assign it to the output folder path used to store the generated materials. 

### **Select Texture Input Folder**

Opens a file dialogue to select the input folder path used for the textures to process. 

### **Select Material Output Folder**

Opens a file dialogue to select the output folder path used to store the generated materials. 

### **Create Materials**

Will generate the new materials and put them into the output folder if all parameters are valid. 




