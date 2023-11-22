export async function uploadFile(file, directory) {
	// set up the request data
	let formData = new FormData()
	formData.append('file', file.file)
	formData.append('directory', directory)
	const url = '/admin/cms/upload';
	// track status and upload file
	file.status = 'loading'
	let response = await fetch(url, { method: 'POST', body: formData })

	// change status to indicate the success of the upload request
	file.status = response.ok

	return response
}

export function uploadFiles(files, url) {
	return Promise.all(files.map((file) => uploadFile(file, url)))
}

export default function createUploader() {
	return {
	  uploadFile: function (file, directory) {
		return uploadFile(file, directory);
	  },
	  uploadFiles: function (files, directory) {
		return uploadFiles(files, directory);
	  },
	};
  }

