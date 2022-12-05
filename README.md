# 401_marievich
# ArcFace
### Сборка Nuget пакета
1. Скачать [нейронную сеть ArcFace](https://github.com/onnx/models/blob/main/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx).
2. Скачать папку `ArcFaceComponent` из текущего репозитория. 
3. Выполнить в папке `ArcFaceComponent` команду 
```
dotnet build
```
4. Из вывода терминала взять путь до пакета и в папке с проектом, выполнить команды
```
dotnet nuget add source --name ArcFaceComponent C:\...\mmariaa12-master\ArcFaceComponent\bin\Debug\ArcFaceComponent.1.0.0.nupkg

dotnet add package ArcFaceNuget
```

