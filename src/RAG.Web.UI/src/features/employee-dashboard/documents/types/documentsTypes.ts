export interface Pit11Document {
  id: string
  name: string
  taxYear: number
  generatedAt: string
  fileName: string
}

export interface Pit11DownloadFile {
  blob: Blob
  fileName: string
}
