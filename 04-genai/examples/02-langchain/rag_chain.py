"""
LangChain LCEL RAG chain with Chroma.

Install:
    pip install langchain-core langchain-openai langchain-chroma langchain-text-splitters

Run:
    export OPENAI_API_KEY=...
    python rag_chain.py
"""

from __future__ import annotations

from langchain_chroma import Chroma
from langchain_core.documents import Document
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate
from langchain_core.runnables import RunnableLambda, RunnablePassthrough
from langchain_openai import ChatOpenAI, OpenAIEmbeddings
from langchain_text_splitters import RecursiveCharacterTextSplitter


RAW_DOCUMENTS = [
    Document(
        page_content=(
            "API keys must be rotated every 90 days. To rotate a key, create a "
            "new key, deploy it, confirm traffic uses the new key, and revoke "
            "the old key."
        ),
        metadata={
            "source_id": "security-api-keys",
            "title": "API Key Policy",
            "url": "https://docs.example.com/security/api-keys",
        },
    ),
    Document(
        page_content=(
            "RAG evaluation should measure context precision, context recall, "
            "answer relevance, faithfulness, and citation accuracy."
        ),
        metadata={
            "source_id": "rag-evaluation",
            "title": "RAG Evaluation Guide",
            "url": "https://docs.example.com/ai/rag-evaluation",
        },
    ),
]


def build_retriever():
    splitter = RecursiveCharacterTextSplitter(chunk_size=350, chunk_overlap=50)
    chunks = splitter.split_documents(RAW_DOCUMENTS)

    embeddings = OpenAIEmbeddings(model="text-embedding-3-small")
    vectorstore = Chroma.from_documents(
        documents=chunks,
        embedding=embeddings,
        collection_name="study_rag_chain",
    )

    return vectorstore.as_retriever(search_kwargs={"k": 4})


def format_docs(docs: list[Document]) -> str:
    return "\n\n".join(
        (
            f"[{doc.metadata['source_id']}] {doc.metadata['title']}\n"
            f"URL: {doc.metadata['url']}\n"
            f"{doc.page_content}"
        )
        for doc in docs
    )


def build_chain():
    retriever = build_retriever()

    prompt = ChatPromptTemplate.from_template(
        """
You are a grounded documentation assistant.
Use only the provided context.
If the answer is not in the context, say you do not know.
Include citations using source IDs in square brackets.

Question:
{question}

Context:
{context}
""".strip()
    )

    model = ChatOpenAI(model="gpt-4.1-mini", temperature=0)

    chain = (
        {
            "question": RunnablePassthrough(),
            "context": retriever | RunnableLambda(format_docs),
        }
        | prompt
        | model
        | StrOutputParser()
    )

    return chain


def main() -> None:
    chain = build_chain()
    answer = chain.invoke("How should API keys be rotated?")
    print(answer)


if __name__ == "__main__":
    main()

