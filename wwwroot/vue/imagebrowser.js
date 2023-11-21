import {defineComponent} from 'vue'
import fileicon from './fileicon.js';
import DropZone from './DropZone.js'
import FilePreview from './FilePreview.js'

// File Management
import useFileList from './compositions/file-list.js'
const { files, addFiles, removeFile } = useFileList()

// Uploader
import createUploader from './compositions/file-uploader.js'
const { uploadFiles } = createUploader('YOUR URL HERE')


export default defineComponent({
  components: {
    'fileicon': fileicon,
    'DropZone': DropZone,
    'FilePreview': FilePreview
  },
  template: `
  <div class="modal-body d-flex" >
    <div class="h-100 w-100 d-flex flex-row">
      <div class="col-2 mr-3 d-flex flex-column">
        <button class="btn btn-primary rounded-0" @click.prevent="uploadFiles(files,currentDir)">Upload</button>
        <DropZone class="drop-area mt-3 h-100" @files-dropped="addFiles" #default="{ dropZoneActive }">
          <label class="d-flex m-0 py-3 button" for="file-input">
            <span v-if="dropZoneActive">
              <span>Drop Them Here</span>
              <span class="smaller">to add them</span>
            </span>
            <span class="m-auto bold" v-else>
              <i class="fa-solid fa-arrow-up-from-bracket fa-xl"></i>
            </span>
            <input type="file" id="file-input" multiple @change="onInputChange" />
          </label>
          <ul class="image-list" v-show="files.length">
            <FilePreview v-for="file of files" :key="file.id" :file="file" tag="li" @remove="removeFile" />
          </ul>
        </DropZone>      
      </div>    
      <fileicon ref="fileicon" :files="fileList" @change:currentdir="refresh($event)" @update:refresh="this.$emit('update:refresh')"></fileicon>
    </div>
  </div>
  `,
  props: ['modelValue'],
  emits: ['update:modelValue', 'update:refresh'],
  data() {
    return {
      imagePairs: [],
      fileList: [],
      baseUrl: "",
      files: files,
      currentDir: "",
    }
  },
  methods: {

    refresh(event){
      this.currentDir=event;
      this.updateImages();
    },

    onInputChange(e) {
      addFiles(e.target.files)
      e.target.value = null // reset so that selecting the same file again will still cause it to fire this change
    },

    uploadFiles(files){
      const vr = this;
      if (this.currentDir!="" && this.currentDir!=null) {
        uploadFiles(files, this.currentDir)
        .then(() => {
          vr.refresh();
          vr.files=[];
        });
      };
    },

    addFiles(files){
      this.files=[];
      addFiles(files);
    },

    removeFile(file){
      removeFile(file);
    },

    GetParentPath(parts, index) {
      let path = "";
      for (let i = 0; i <= index; i++) {
        path += "/" + parts[i];
      }
      return path;
    },
            
    generateFileList(filePairs) {
      const fileList = [];
  
      filePairs.forEach((path) => {
          const parts = path.webp.split("/");
          let currentDir = fileList;
          let parentDir = null;
  
          parts.forEach((part, index) => {
              const isDirectory = index < parts.length - 1;
              let fileType = isDirectory ? "folder" : null;
  
              if (!isDirectory) {
                  if (path.png) fileType = "png";
                  else if (path.jpg) fileType = "jpg";
                  else if (path.svg) fileType = "svg";
              }
  
              let existingNode = currentDir.find((node) => node.name === part);
  
              if (!existingNode) {
                  const newNode = {
                      name: part,
                      isDirectory,
                      children: [],
                      fileType: fileType,
                      parent: parentDir, // Set the parent property to the parent directory
                      fullpath: this.GetParentPath(parts, index),
                  };
                  currentDir.push(newNode);
                  currentDir = newNode.children;
                  parentDir = newNode; // Update the parent directory for the next iteration
              } else {
                  currentDir = existingNode.children;
                  parentDir = existingNode; // Update the parent directory for the next iteration
              }
          });
      });
  
      return fileList;
    },      
            
    getfilepath(path) {
      const index = path.lastIndexOf("/");
      return path.substring(index + 1);
    },

    getfilename(path) {
      //regex with forward slash
      const regex = (/\/([^\/]*)$/);
      const result = path.match(regex);
      return result[1]+".webp";
    },

    cleanPath(filename){
      let returnPath = filename.replace(/\\/g, '/');
      const themesIndex = returnPath.indexOf("Themes");
      if (themesIndex !== -1) {
        returnPath=returnPath.substring(themesIndex);
      }
      returnPath=returnPath.replace("/wwwroot", "");
      //remove extension
      returnPath=returnPath.replace(/\.[^/.]+$/, "");
      return returnPath;
    },

    deserialize(data) {
      let imagePairs = [];
      const webpWithoutExtension = data.Value.webp.map(this.cleanPath);
      const jpgsWithoutExtension = data.Value.jpgs.map(this.cleanPath);
      const pngWithoutExtension = data.Value.pngs.map(this.cleanPath);
      for (const item of webpWithoutExtension) {
        if (jpgsWithoutExtension.includes(item)) {
          imagePairs.push({ webp: item, jpg: item });
        }
        if (pngWithoutExtension.includes(item)) {
          imagePairs.push({ webp: item, png: item });
        }
      }
      this.imagePairs = imagePairs;
    },

    async getImages() {
        return fetch("/admin/cms/getimages")
        .then(response => {
          if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
          }
          return response.json();
        })
        .then(data => {
          return data;
        })
        .catch(e => {
          console.log(e);
          return null;
        });
    },

    async updateImages() {
      this.getImages()
      .then(data => {
        this.deserialize(data);
        this.fileList=this.generateFileList(this.imagePairs);
      });
    },
  },
  mounted() {
    this.baseUrl = `${window.location.protocol}//${window.location.hostname}${window.location.port ? `:${window.location.port}` : ''}`;
    this.updateImages();
    $('[data-toggle="tooltip"]').tooltip()
  }
});             