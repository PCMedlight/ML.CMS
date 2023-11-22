import {defineComponent} from 'vue'

export default defineComponent({
  name:'fileicon',
    template: `
    <div class="col d-flex flex-column">
      <div class="d-flex flex-row mb-3">
        <button type="button" class="btn btn-white btn-sm mr-3" @click="movedirectoryup" style="width:40px;">
          <i class="fa-solid fa-turn-up"></i>
        </button>
        <div class="border p-2 align-middle w-100">{{currentdir}}</div>
      </div>
      <div class="d-flex flex-row" style="height:60vh">
        <div class="p-2 d-flex flex-column border col-2 flex-grow-1 overflow-y-scroll">
          <div :class="!item.isopen ? 'text-black' : 'text-primary'" role=button class="my-1 px-2" v-for="item in onlyFolders" :key="item.name" @click="selectdirectory($event,item)">
            📁 {{ item.name }}
          </div>
        </div>
        <div class="p-2 ml-2 d-flex flex-column border col-4 flex-grow-1 overflow-y-scroll position-relative">
          <div class="border d-flex flex-row mb-2 imgfile" v-for="item in onlyFiles" :key="item.name" :class="isselected(item.fullpath) ? 'border-info' : ''">
            <span @click="selectfile($event,item)" class="flex-grow-1" role=button>
              <img v-if="!item.isDirectory" :src="item.fullpath + '.webp'" class="fit-contain border-right" width="40" height="40" style="object-fit:contain;"/>
              <span class="ml-2 small my-auto">{{ item.name }}</span>
            </span>
            <button @click="deleteFile(item)" class="ml-auto my-auto rounded-5 border-0 mr-2 bg-transparent">
              <i class="fa-solid fa-xmark"></i>
            </button>
          </div>
        </div>
        <div class="col-5 pl-3">
            <div class="border bg-light w-100 position-relative d-flex" style="height:150px;">
              <i v-if="selectedFile.name == 'Select an image'" class="fa-solid fa-image fa-xl m-auto"></i>
              <img v-else class="w-100 h-100 mb-3 fit-contain position-absolute" :src="selectedFile.fullpath + '.webp'"  style="object-fit:contain;"/>
            </div>
            <p class="font-weight-bold pt-3">{{ cmsid }}</p>            
            <div class="lead my-2">{{ selectedFile.name }}</div>
            <hr/>
            <div class="small my-2">{{ selectedFile.fullpath }}.{{ selectedFile.fileType }}</div>
            <hr/>
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">source</label>            
              <input v-model="loadedtag.src" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div>              
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">picture</label>            
              <input v-model="loadedtag.picatts" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div>               
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">img class</label>            
              <input v-model="loadedtag.imgclass" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div>
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">img atts</label>            
              <input v-model="loadedtag.imgatts" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div>                        
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">Alt EN</label>            
              <input v-model="loadedtag.EN" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div> 
            <div class="d-flex flex-row my-2"> 
              <label class="small lh-1 mt-2 mb-1" style="white-space:nowrap; width:75px;">Alt DE</label>            
              <input v-model="loadedtag.DE" type="text" class="form-control rounded-0" placeholder="Entry" style="height:25px">            
            </div> 
            <hr/>
            <div class="form-check form-switch my-3">
              <input v-model=lazyload class="form-check-input"  type="checkbox" role="switch" id="switchlazyload">
              <label class="form-check-label" for="switchlazyload">Lazy load</label>
            </div> 
            <button type="button" class="btn btn-white btn-sm rounded-0 mr-2" @click="SavePicture">
            <i class="fa-regular fa-floppy-disk"></i>Save
          </button>    
        </div>            
      </div>
    </div>
    `,
    props: {
  
      files: {type: Array, default: () => [] },
      displayChildren: {type: Array, default: () => []}
    },
    emits: ['selected:modelValue','change:currentdir', 'update:refresh'],

    data() {
      
      return {
        currentdir: "Themes/MEDlight-Theme/images/general",
        selectedFile: {
          name: "Select an image",
          fullpath: "https://placehold.co/600x400?text=Image"
        },
        cmsid: "",
        lazyload: true,
        loadedtag:{
          picatts: "",
          imgclass: "",
          imgatts: "",
          src: "",
          EN: "",
          DE: "",
          lazyload: true
        }          
      }

    },

    methods: {

      SavePicture(){
        if (this.outputPictureHTML){
          const data = {
              ID: this.cmsid,
              "en-US": this.outputPictureHTML.EN.outerHTML,
              "de-DE": this.outputPictureHTML.DE.outerHTML,
            };
          const apiUrl = '/admin/cms/UpdateResource/';
          const request = {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
          };        
          fetch(apiUrl, request)
            .then(response => {
              return response.json();
            })
            .then(responseData => {
              if (!responseData.Success) {
                throw new Error(responseData.Message);
              }
              window.displayNotification("POST request successful. Response data: " +responseData.Message, "success", false, 3000);
              this.$emit('update:refresh', this.outputPictureHTML);
            })
            .catch(error => {
              console.error('Error:', error);
              window.displayNotification("Error during POST request: "+ error, "error", false, 3000);
            });
        }
      },

      deconstruct(htmlString){
        const ENHTML = htmlString.EN;
        const parser = new DOMParser();
        const doc = parser.parseFromString(ENHTML, 'text/html');
        const imgelement = doc.body.querySelector('img');
        const picelement =  doc.body.querySelector('picture');
        this.loadedtag.picatts = '';
        if (picelement){
          const picatts = picelement.getAttributeNames();
          picatts.forEach((att) => {
            const attvalue=picelement.getAttribute(att);
            if (attvalue == null || attvalue == '') this.loadedtag.picatts += att;
            else this.loadedtag.picatts += att + '="' + attvalue + '" ';
          });
        };
        this.loadedtag.EN = imgelement ? imgelement.getAttribute('alt') : "";
        if (this.loadedtag.EN == null) this.loadedtag.EN = "";
        this.loadedtag.src = imgelement ? imgelement.getAttribute('src') : "";
        this.loadedtag.imgclass = imgelement ? imgelement.getAttribute('class') : "";
        if (imgelement){
          const imgatts = imgelement.getAttributeNames();
          imgatts.forEach((att) => {
            if (att == "src" || att == "class" || att == "alt" || att == "loading") return;
            const attvalue=imgelement.getAttribute(att);
            if (attvalue == null || attvalue == '') this.loadedtag.imgatts += att;
            else this.loadedtag.imgatts += att + '="' + attvalue + '" ';
          });
        };
        this.loadedtag.lazyload = imgelement ? imgelement.getAttribute('loading') == "lazy" : true;
        
        const DEHTML = htmlString.DE;
        const parserDE = new DOMParser(); 
        const docDE = parser.parseFromString(DEHTML, 'text/html');
        const imgelementDE = docDE.body.querySelector('img');       
        this.loadedtag.DE = imgelementDE ? imgelementDE.getAttribute('alt') : "";
        if (this.loadedtag.DE == null) this.loadedtag.DE = "";
    },

      async RequestImageResource(queryIds = [""]) {
        const apiUrl = '/admin/cms/GetLanguageResource/';
        const data = { id: queryIds };
        const requestOptions = {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(data),
        };
      
        try {
          const response = await fetch(apiUrl, requestOptions);    
          if (!response.ok) throw new Error('Failed to send POST request');    
          const responseData = await response.json();
          console.log(responseData.Message);    
          return responseData;
        } catch (error) {
          console.error('Error:', error);
          throw error;
        }
      },
      
      GetSRCstring(htmlString){
        const regex = /src="([^"]*)"/;
        const sourcestring = htmlString.match(regex);
        if (!sourcestring) return "";
        const filewithoutextension = sourcestring[1].replace(/\.[^/.]+$/, "");
        return filewithoutextension;
      },

      loadFileFromPath(src){
        let srcpath = src.split("/");
        if (srcpath[0] === "") {srcpath.shift();}
        let currentFile = this.files[0];
        for (let i = 1; i < srcpath.length; i++) {
          currentFile = currentFile.children.find((file) => {
            return file.name.toLowerCase() == srcpath[i].toLowerCase();
          });
          if (currentFile == null) break;
        };
        if (currentFile != null) {
          this.selectdirectory(null,currentFile);
          this.selectfile(null,currentFile);
        }
      },

      async init(id = "") {
        const response = await this.RequestImageResource(id);
        const resource = response.Message[0].Value;
        this.cmsid = id[0];
        console.log(resource);
        const src=this.GetSRCstring(resource.EN);
        if (src != "") {
          this.loadFileFromPath(src);
          this.deconstruct(resource);
        }
        else {
          this.loadedtag={
            picatts: "",
            imgclass: "",
            imgatts: "",
            src: "",
            EN: "",
            DE: "",
            lazyload: true
          };
          this.selectedFile= {
            name: "Select an image",
            fullpath: "https://placehold.co/600x400?text=Image"
          };
        };
        $('#imageBrowserModal').modal('show');
        const backdrop = document.getElementsByClassName('modal-backdrop');
        if (backdrop[0]) backdrop[0].remove(); 
      },
      
      isselected(fullpath){
        return (this.selectedFile.fullpath == fullpath);
      },

      sendDelete(deleteFile) {
        const url = '/admin/cms/deletefile';
        let formData = new FormData();
        formData.append('fileUrl', deleteFile);
        let response = fetch(url, { method: 'POST', body: formData });
        console.log (response);
      },      

      deleteFile(file) {
        const confirmation = confirm("Are you sure you want to delete this file?");
        if (confirmation) {
          let deleteFile = file.fullpath + "." + file.fileType;
          deleteFile = deleteFile.replace("/Themes/MEDlight-Theme/images/", "");
          this.sendDelete(deleteFile);
          let deleteWebp = file.fullpath + ".webp";
          deleteWebp = deleteWebp.replace("/Themes/MEDlight-Theme/images/", "");
          this.sendDelete(deleteWebp);
          this.init([this.cmsid]);
        }
      },

      selectfile(event,item) {
          this.selectedFile = item;
          this.$emit('selected:modelValue', item);
      },

      movedirectoryup() {
        const parts = this.currentdir.split("/");
        if (parts.length > 3) {
          parts.pop();
        }
        this.currentdir = parts.join("/");
        this.$emit('change:currentdir', this.currentdir);
      },

      selectdirectory(event,item) {
        let newdir = item.fullpath;
        if (!item.isDirectory) {
          newdir = item.parent.fullpath;
        }
        this.currentdir = newdir;
        this.$emit('change:currentdir', this.currentdir);
      },

      extractAttributes(string) {
        const parser = new DOMParser();
        const parsestring = "<div " + string + "></div>";
        const doc = parser.parseFromString(parsestring, 'text/html');
        const element = doc.body.querySelector('div');
        const atts = element.getAttributeNames();
        let returnatts = [];
        for (const att of atts) {
          const attValue = element.getAttribute(att);
          if (attValue == null || attValue == '') returnatts.push(att);
          else
          returnatts.push(att + '="' + attValue + '"');
        }
        return returnatts;        
      },

    },

    computed: {

      outputPictureHTML() {      
        if (this.selectedFile.name !== "Select an image") {
          const picture = document.createElement('picture'); 
          const source = document.createElement('source');
          //add atributes to picture
          const picatts = this.extractAttributes(this.loadedtag.picatts);
          picatts.forEach((att) => {
            const attsplit = att.split("=");
            if (attsplit.length > 1) {
              picture.setAttribute(attsplit[0], attsplit[1].replace(/"/g, ''));
            }
            if (attsplit.length == 1) {
              picture.setAttribute(attsplit[0], "");
            }
          });
          source.setAttribute("srcset", this.selectedFile.fullpath + ".webp");
          source.setAttribute("type", "image/webp");      
          const img = document.createElement('img');
          img.setAttribute("class", this.loadedtag.imgclass);
          img.setAttribute("src", this.selectedFile.fullpath + "." + this.selectedFile.fileType);
          img.setAttribute("alt", this.loadedtag.EN);
          img.setAttribute("loading", this.lazyload ? "lazy" : "eager");
          //add atributes to img
          const imgatts = this.extractAttributes(this.loadedtag.imgatts);
          imgatts.forEach((att) => {
            const attsplit = att.split("=");
            if (attsplit.length > 1) {
              img.setAttribute(attsplit[0], attsplit[1].replace(/"/g, ''));
            }
            if (attsplit.length == 1) {
              img.setAttribute(attsplit[0], "");
            }
          });  
          picture.appendChild(source);
          picture.appendChild(img);
          const EN = picture;
          const DE = picture.cloneNode(true);
          DE.querySelector('img').setAttribute("alt", this.loadedtag.DE);
          return {EN, DE};
        }      
        return null;
      },


      onlyFiles() {
        return this.currentFolder.filter((file) => {
          return !file.isDirectory;
        });
      },

      onlyFolders() {
        let returnFolder = this.currentFolder.filter((file) => {
          return file.isDirectory;
        });
        const containsFolders = returnFolder.some((file) => {
          return file.isDirectory;
        });
        if (!containsFolders && this.currentFolder.length > 0) {
          returnFolder = this.currentFolder[0].parent.parent.children.filter((file) => {
            return file.isDirectory;
          });
          const currentDir = this.currentdir.split("/").pop();
          returnFolder.forEach((folder) => {
            if (folder.name === currentDir) {
              folder.isopen = true;
            }
            else {
              folder.isopen = false;
            }
          });
          return returnFolder;
        }
        else {
          return returnFolder;
        } 
      },

      currentFolder() {
        if (!this.files || this.files.length === 0) {
          return [];
        }
        const parts = this.currentdir.split("/");
        if (parts[0] === "") {parts.shift();}
        let currentFolder = this.files;
      
        for (let i = 0; i < parts.length; i++) {
          currentFolder = currentFolder.find((file) => {
            return file.name.toLowerCase()  == parts[i].toLowerCase() ;
          });
      
          if (currentFolder) {
            currentFolder = currentFolder.children;
          }
        }

        if (currentFolder){
          currentFolder.sort((a,b) => {
            if (a.isDirectory && !b.isDirectory) return -1;
            if (!a.isDirectory && b.isDirectory) return 1;
            return 0;
          });
        }
        return currentFolder;
      },

      
    },


}); 

