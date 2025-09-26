import React from 'react'
import { useParams } from 'react-router-dom'

export default function QuizDetail() {
  const { id } = useParams()

  return (
    <div>
      <h3 className="text-2xl font-bold mb-4">Quiz: {id}</h3>
      <p className="text-sm text-gray-600">Szczegóły quizu / podejmowanie quizu.</p>
    </div>
  )
}
