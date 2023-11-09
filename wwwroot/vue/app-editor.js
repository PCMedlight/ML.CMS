import { createApp, onMounted, ref, computed, reactive, watch, watchEffect } from 'vue'


createApp({
    components: {

    },

    data() {
        return {
            iframe: null,
            cmselements:[],
            entryid: "",
            entrytag:"",
            entryclasses: "",
            entryen: "",
            entryde: "",
            highlightselected: true,
        };
    },

    methods: {

        togglehighlightmissing(event){
            if (event.target.checked) {
                for (const element of this.cmselements) {
                    console.log(element.Value);
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

        deconstruct(htmlString){
            const parser = new DOMParser();
            const doc = parser.parseFromString(htmlString, 'text/html');
            const element = doc.body.querySelector('*');
            if (element == null) {
                return {elementType: "text", classes: "", innerHTML: htmlString};
            }
            const elementType = element.tagName.toLowerCase(); 
            let classes = element.getAttribute('class');
            if (classes == null) {
                classes = "";
            }   
            const innerHTML = element.textContent;
            return {elementType, classes, innerHTML};
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

        loadform(element){
            const deconstructedEN = this.deconstruct(element.EN);
            this.entrytag = deconstructedEN.elementType;
            this.entryid = element.ID;
            this.entryclasses = deconstructedEN.classes;
            this.entryen = deconstructedEN.innerHTML;
            const deconstructedDE = this.deconstruct(element.DE);
            this.entryde = deconstructedDE.innerHTML;
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
            let contentEN = this.entryen;
            let contentDE = this.entryde;

            const tag = this.entrytag;
            if (tag != "text" && tag != "") {       
                const element = document.createElement(tag);
                const classes = this.entryclasses.split(' ');
                classes.forEach(className => {
                    if (className.trim() !== "") {
                      element.classList.add(className);
                    }
                  });
                element.innerHTML = contentEN;
                contentEN = element.outerHTML;
                element.innerHTML = contentDE;
                contentDE = element.outerHTML;
            }
            const data = {
                ID: id,
                "en-US": contentEN,
                "de-DE": contentDE,
              };
        
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

        updateresource(id = "", content = "") {
            const vr = this;
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
                vr.iframe.contentWindow.location.reload();
              })
              .catch(error => {
                console.error('Error:', error);
                window.displayNotification("Error during POST request: "+ error, "error", false, 3000);
              });
        },

        loadpartial() {
            const vr = this;
            const url = window.location.origin;
            const iframe = document.createElement('iframe');
            iframe.width = '100%';
            iframe.style.height = '80vh';
            iframe.src = url;
            iframe.frameborder = '0';
        
            const container = document.getElementById('iframeContainer');
            container.appendChild(iframe);
            this.iframe = iframe;

            // Add a load event listener to the iframe
            iframe.addEventListener('load', function () {
                const iframeDoc = iframe.contentDocument;
                const dataelements= iframeDoc.querySelectorAll('[data-cms]');
                const ids=[];
                for (const element of dataelements) {
                    ids.push(element.getAttribute('data-cms'));
                }
                let returneddata = null;
                vr.getresource(ids);  
                
                /*iframe.addEventListener('load', function() {
                    const iframeURL = iframe.contentWindow.location.href;
                    console.log('Current URL of the iframe:', iframeURL);
                  });   */              

                iframeDoc.addEventListener('click', function (event) {
                    const clickedElement = event.target;
                    
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

        plaintext(){
            if (this.entrytag == "text" || this.entrytag == "") {
                return false;
            }
            else return true;
        }

    },
    mounted() {

        this.loadpartial();

    },
}).mount('#app-editor');