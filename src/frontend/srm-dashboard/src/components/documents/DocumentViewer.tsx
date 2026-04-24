import React, { useState, useEffect } from 'react';
import { Loader2, FileWarning, RotateCw, Maximize2, Zap } from 'lucide-react';

// ✅ Cette interface doit être exportée pour être vue par les autres composants
export interface DocumentViewerProps {
  streamUrl: string; 
  title?: string;
  className?: string;
}

/**
 * SRE Stream Viewer - Orchestrateur de flux binaire
 * Reçoit une URL de flux (streamUrl) et gère l'affichage inline.
 */
const DocumentViewer: React.FC<DocumentViewerProps> = ({ 
  streamUrl, 
  title = "Aperçu Document", 
  className = "" 
}) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [rotation, setRotation] = useState(0);

  // Reset du viewer si l'URL change (passage d'un document à un autre)
  useEffect(() => {
    setLoading(true);
    setError(false);
  }, [streamUrl]);

  const handleRotate = () => setRotation((prev) => (prev + 90) % 360);

  return (
    <div className={`relative flex flex-col bg-slate-950 rounded-xl border border-slate-800 overflow-hidden shadow-2xl ${className}`}>
      {/* Header technique du flux */}
      <div className="flex items-center justify-between p-3 bg-slate-900/95 border-b border-slate-800 z-10">
        <div className="flex items-center gap-3">
          <div className="w-6 h-6 rounded bg-blue-500/10 flex items-center justify-center">
            <Zap className="w-3.5 h-3.5 text-blue-400" />
          </div>
          <span className="text-xs font-bold text-slate-200 truncate max-w-[200px]">
            {title}
          </span>
        </div>
        
        <div className="flex items-center gap-1">
          <button 
            onClick={handleRotate} 
            className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-all"
            title="Rotation"
          >
            <RotateCw className="w-4 h-4" />
          </button>
          <a 
            href={streamUrl} 
            target="_blank" 
            rel="noopener noreferrer" 
            className="p-2 text-slate-400 hover:text-blue-400 rounded-lg"
          >
            <Maximize2 className="w-4 h-4" />
          </a>
        </div>
      </div>

      {/* Zone de rendu Iframe */}
      <div className="relative flex-1 min-h-[500px] bg-slate-900 flex items-center justify-center">
        {loading && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-950/90 z-20">
            <Loader2 className="w-10 h-10 animate-spin text-blue-500 mb-2" />
            <p className="text-[10px] font-mono text-slate-500 uppercase">Streaming Data...</p>
          </div>
        )}

        {error ? (
          <div className="text-center p-8">
            <FileWarning className="w-12 h-12 text-rose-500/30 mx-auto mb-4" />
            <p className="text-white text-sm font-bold">Erreur de flux binaire</p>
            <p className="text-slate-500 text-[10px] mt-1 font-mono">Status: 404/Null</p>
          </div>
        ) : (
          <iframe
            src={`${streamUrl}#toolbar=0`}
            className="w-full h-full border-none transition-all duration-300"
            style={{ 
              transform: `rotate(${rotation}deg) scale(${rotation % 180 !== 0 ? 0.8 : 1})`,
              opacity: loading ? 0 : 1
            }}
            onLoad={() => setLoading(false)}
            onError={() => { setError(true); setLoading(false); }}
            title="SRE Binary Stream"
          />
        )}
      </div>
      
      <div className="p-2 bg-slate-900/80 border-t border-slate-800 text-[9px] font-mono text-slate-600 flex justify-between">
        <span>GATEWAY_CONNECTED</span>
        <span>BYTE_RANGE: ACTIVE</span>
      </div>
    </div>
  );
};

export default DocumentViewer;