import { ref, onMounted, onUnmounted, defineEmits } from 'vue'

export default {
template: `
        <div :data-active="active" @dragenter.prevent="setActive" @dragover.prevent="setActive" @dragleave.prevent="setInactive" @drop.prevent="onDrop">
            <slot :dropZoneActive="active"></slot>
        </div>
    `,
  props: [],
  emits: ['files-dropped'],
  
  setup(_, { emit }) {
    const active = ref(false)
    let inActiveTimeout = null

    const setActive = () => {
      active.value = true
      clearTimeout(inActiveTimeout)
    }

    const setInactive = () => {
      inActiveTimeout = setTimeout(() => {
        active.value = false
      }, 50)
    }

    const onDrop = (e) => {
      setInactive()
      emit('files-dropped', [...e.dataTransfer.files])
    }

    const preventDefaults = (e) => {
      e.preventDefault()
    }

    const events = ['dragenter', 'dragover', 'dragleave', 'drop']

    onMounted(() => {
      events.forEach((eventName) => {
        document.body.addEventListener(eventName, preventDefaults)
      })
    })

    onUnmounted(() => {
      events.forEach((eventName) => {
        document.body.removeEventListener(eventName, preventDefaults)
      })
    })

    return {
      active,
      setActive,
      setInactive,
      onDrop,
      preventDefaults
    }
  }
}