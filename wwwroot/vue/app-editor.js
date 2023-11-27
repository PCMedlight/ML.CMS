import { createApp, onMounted, ref, computed, reactive, watch, watchEffect } from 'vue'
import imagebrowser from './imagebrowser.js'
import * as xmlJs from '../lib/xml-js.esm.js';



createApp({
    components: {
        "imagebrowser" : imagebrowser
    },

    data() {
        return {
            iframe: null,
            cmselements:[],
            cmsimages:[],
            entryid: "",
            entrytag:"",
            entryen: "",
            entryde: "",
            highlightselected: true,
            hoverelement: null,

            imgsrc: null,
            imgalten: null,
            imgaltde: null,
        };
    },

    methods: {


        exportDE() {
            // Fetch the XML content from the server
            fetch('/admin/cms/ExportLanguageResource', {
              method: 'GET',
              headers: { 'Content-Type': 'application/json' },
              })
            .then(response => {
              if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
              }
              return response.json();
            })
            .then(responseData => {
                // Extract the XML string from the nested structure
                const xmlResponse = responseData.Message[1];

                const blob = new Blob([xmlResponse], { type: 'application/xml' });

                const downloadLink = document.createElement('a');
                downloadLink.href = window.URL.createObjectURL(blob);
                downloadLink.download = 'resources.de-de.xml';
            
                // Append the link to the document
                document.body.appendChild(downloadLink);
            
                // Trigger a click on the link to start the download
                downloadLink.click();
            
                // Remove the link from the document
                document.body.removeChild(downloadLink);
            })
            .catch(error => console.error('Error:', error));
          },        
     
        exportEN() {
            // Fetch the XML content from the server
            fetch('/admin/cms/ExportLanguageResource', {
              method: 'GET',
              headers: { 'Content-Type': 'application/json' },
              })
            .then(response => {
              if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
              }
              return response.json();
            })
            .then(responseData => {
                // Extract the XML string from the nested structure
                const xmlResponse = responseData.Message[0];

                const blob = new Blob([xmlResponse], { type: 'application/xml' });

                const downloadLink = document.createElement('a');
                downloadLink.href = window.URL.createObjectURL(blob);
                downloadLink.download = 'resources.en-us.xml';
            
                // Append the link to the document
                document.body.appendChild(downloadLink);
            
                // Trigger a click on the link to start the download
                downloadLink.click();
            
                // Remove the link from the document
                document.body.removeChild(downloadLink);
            })
            .catch(error => console.error('Error:', error));
          },
          

        togglehighlightmissing(event){
            if (event.target.checked) {
                for (const element of this.cmselements) {
                    if (element.Value.EN == "Not found" && element.Value.DE == "Not found") {
                        const queryElement = this.iframe.contentDocument.querySelectorAll('[data-cms="'+element.Value.ID+'"]');
                        queryElement.forEach(element => {
                            element.setAttribute('data-cms-missing', 'true');
                        });
                    }
                    if (element.Value.EN == "Not found" ^ element.Value.DE == "Not found") {
                        const queryElement = this.iframe.contentDocument.querySelectorAll('[data-cms="'+element.Value.ID+'"]');
                        for (const element of queryElement) {
                            element.setAttribute('data-cms-incomplete', 'true');
                        }
                    }
                    else {
                        const queryElement = this.iframe.contentDocument.querySelectorAll('[data-cms="'+element.Value.ID+'"]');
                        for (const element of queryElement) {
                            element.setAttribute('data-cms-complete', 'true');
                        }
                    }
                }
            }
            else
            {
                const queryElements = this.iframe.contentDocument.querySelectorAll('[data-cms]');
                for (const element of queryElements) {
                    element.removeAttribute('data-cms-missing');
                    element.removeAttribute('data-cms-incomplete');
                    element.removeAttribute('data-cms-complete');
                }
            }
        },

        refreshiframe(){
            this.iframe.contentWindow.location.reload();
        },

        togglehighlightselected(event){
            const deselectElements = this.iframe.contentDocument.querySelectorAll('[data-cms-visible]');
            for (const element of deselectElements) {
                element.removeAttribute('data-cms-visible');
            }
            if (this.highlightselected) {
                const queryElement = this.iframe.contentDocument.querySelectorAll('[data-cms="'+this.entryid+'"]');
                for (const element of queryElement) {
                    element.setAttribute('data-cms-visible', 'true');
                }
            }
        },

        extractAltAttribute(htmlString) {
        const altRegex = /alt="([^"]*)"/;
        const match = htmlString.match(altRegex);
        if (match) {
        const altValue = match[1];
        return altValue;
        } else {
        return null;
        }
        },

        loadform(element){
            this.entryid = element.ID;
            this.entryen = element.EN;
            this.entryde = element.DE;
            this.togglehighlightselected(null);
        },

        incomplete(item){
            if (item.EN == "Not found") {
                return "border-danger";
            }
            if (item.DE == "Not found") {
                return "border-warning";
            }
            return "border-success";
        },

        getresource(queryIds = [""]) {
            const apiUrl = '/admin/cms/GetLanguageResource/';
            const data = {id:queryIds};
            const requestOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', 
            },
                body: JSON.stringify(data),
            };
            fetch(apiUrl, requestOptions)
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                throw new Error('Failed to send POST request');
                }
            })
            .then(data => {
                const returndata = data;
                this.cmselements=(returndata.Message);
                console.log('POST request successful:', this.cmselements);
            })
            .catch(error => {
                console.error('Error:', error);
            });
        },

        serializeUpdateData(){
            const id = this.entryid;
            const data = {
                ID: id,
                "en-US": this.entryen,
                "de-DE": this.entryde,
              };

            if (data['de-DE'] == "Not found") {
                delete data['de-DE'];
            }
            if (data['en-US'] == "Not found") {
                    delete data['en-US'];
            }        
            return data;
        },

        getCMSElement(id){
            for (const element of this.cmselements) {
                if (element.Value.ID == id) {
                    return element.Value;
                }
            }
            return null;
        },

        hover(item){
            console.log(item);
        },

        endhover(item){
            console.log(item);
        },

        scrollto(item){
            console.log(item);
            const queryElement = this.iframe.contentDocument.querySelectorAll('[data-cms="'+item.Value.ID+'"]');
            console.log(queryElement);
            console.log('[data-cms="'+item.Value.ID+'"]');
            queryElement[0].scrollIntoView({behavior: "smooth", block: "center", inline: "nearest"});
        },

        updateresource() {
            const iframeURL= this.iframe.src;
            console.log('Current URL of the iframe:', iframeURL);

            const apiUrl = '/admin/cms/UpdateResource/';
            const data = this.serializeUpdateData();
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
                this.refreshiframe();
              })
              .catch(error => {
                console.error('Error:', error);
                window.displayNotification("Error during POST request: "+ error, "error", false, 3000);
              });
        },

        launchimagebrowser(event){
            //preven default behaviour
            event.preventDefault();
            const id = [event.target.getAttribute('data-cms-img')];
            this.$refs.imgbrowserref.$refs.fileicon.init(id);
        },

        loadpartial() {
            const vr = this;
            const url = window.location.origin;
            const iframe = document.createElement('iframe');
            iframe.width = '100%';
            iframe.height = '100%';
            iframe.style.overflow = 'clip';
            iframe.src = url;
            iframe.frameborder = '01';
        
            const container = document.getElementById('iframeContainer');
            container.appendChild(iframe);
            this.iframe = iframe;

            iframe.addEventListener('load', function () {
               const iframeDoc = iframe.contentDocument;

               const stylecontainer = document.getElementById('iframestyle');
               const clonedstylecontainer= stylecontainer.cloneNode(true);
               const iframeHead = iframeDoc.head || iframeDoc.getElementsByTagName('head')[0];
               iframeHead.appendChild(clonedstylecontainer);

                const dataelements = iframeDoc.querySelectorAll('[data-cms]');
                const ids = [];

                for (const element of dataelements) {
                    ids.push(element.getAttribute('data-cms'));
                }

                vr.getresource(ids);
                iframeDoc.addEventListener('click', function (event) {
                    const clickedElement = event.target;

                    if (clickedElement.hasAttribute('data-cms-img')) {
                        vr.launchimagebrowser(event);
                    }

                    
                    if (clickedElement.hasAttribute('data-cms')) {
                        const datacms = clickedElement.getAttribute('data-cms');
                        const id = vr.getCMSElement(datacms);
                        vr.loadform(id);
                    }
                });
            });
        },


        cmselementname(element){
            let name = element.getAttribute('data-cms');
            if (name == "") {
                return "Invalid"
            }
            else return name;
        },
       

    },
    computed: {

        currenthoverelement(){
            if (this.iframe == null) {
                return null;
            }
            this.iframe.contentDocument.querySelectorAll('[data-cms]').forEach(element => {
                element.removeAttribute('data-cms-hover');
            });
            if (this.hoverelement == null) {
                return null;
            }
            this.iframe.contentDocument.querySelectorAll('[data-cms]').forEach(element => {
                if (element.getAttribute('data-cms') == this.hoverelement.Value.ID) {
                    element.setAttribute('data-cms-hover', 'true');
                }
            });
            return this.hoverelement.Value.ID;
        },

        cmsuntrackedelements(){
            const idelement = this.cmselements;
            if (this.iframe == null) {
                return [];
            }
            const textelements = this.iframe.contentDocument.getElementById('cms-tracker');
            if (textelements == null) {
                return [];
            }
            const output = [];
            //if and element is found in the cms tracker, remove it from idelement
            for (const element of this.cmselements) {
                const id = element.Value.ID;
                const cms = textelements.querySelector('[data-cms="'+id+'"]');
                if (cms == null) {
                    output.push(element);
                }
            }

            return output;
        },

        cmstrackerelements(){
            const idelement = this.cmselements;
            if (this.iframe == null) {
                return [];
            }
            const textelements = this.iframe.contentDocument.getElementById('cms-tracker');
            if (textelements == null) {
                return [];
            }
            const output = [];
            //if and element is found in the cms tracker, add it from idelement
            for (const element of textelements.children) {
                const id = element.getAttribute('data-cms');
                const cms = idelement.find(x => x.Value.ID == id);
                if (cms != undefined) {
                    output.push(cms);
                }

            }

            return output;
        },


    },
    mounted() {

        this.loadpartial();

    },
}).mount('#app-editor');