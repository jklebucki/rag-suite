import React from 'react'
import { Link } from 'react-router-dom'

export function LandingPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center">
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        {/* Logo */}
        <div className="mb-8">
          <img
            src="/logo-citronex-2x.png"
            alt="RAG Suite Logo"
            className="mx-auto h-24 w-auto"
          />
        </div>

        {/* Main Heading */}
        <h1 className="text-4xl md:text-6xl font-bold text-gray-900 mb-6">
          Welcome to <span className="text-indigo-600">RAG Suite</span>
        </h1>

        {/* Subtitle */}
        <p className="text-xl md:text-2xl text-gray-600 mb-12 max-w-2xl mx-auto">
          Advanced Retrieval-Augmented Generation platform for intelligent document processing and conversational AI.
        </p>

        {/* Call to Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link
            to="/login"
            className="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-3 px-8 rounded-lg transition duration-300"
          >
            Get Started
          </Link>
          <Link
            to="/about"
            className="bg-white hover:bg-gray-50 text-indigo-600 font-semibold py-3 px-8 rounded-lg border border-indigo-600 transition duration-300"
          >
            Learn More
          </Link>
        </div>

        {/* Features Preview */}
        <div className="mt-16 grid md:grid-cols-3 gap-8">
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Document Ingestion</h3>
            <p className="text-gray-600">Seamlessly upload and process various document formats with advanced parsing capabilities.</p>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Intelligent Search</h3>
            <p className="text-gray-600">Hybrid search combining lexical and vector-based retrieval for accurate results.</p>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Conversational AI</h3>
            <p className="text-gray-600">Engage in natural conversations powered by state-of-the-art language models.</p>
          </div>
        </div>
      </div>
    </div>
  )
}